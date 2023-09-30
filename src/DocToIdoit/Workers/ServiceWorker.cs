using IronOcr;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace DocToIdoit
{
    /// <summary>
    /// Main Worker
    /// </summary>
    public sealed class ServiceWorker : BackgroundService
    {
        private readonly ILogger<ServiceWorker> _logger;
        private readonly Timer _timer;
        private readonly IServiceProvider _services;
        private readonly IConfiguration _configuration;
        private readonly IOptionsMonitor<List<Product>> _productOptionsMonitor;
        private bool _operationRunning;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServiceWorker"/> class.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="configuration"></param>
        /// <param name="services"></param>
        /// <param name="lifetime"></param>
        /// <param name="productOptionsMonitor"></param>
        public ServiceWorker(ILogger<ServiceWorker> logger, IConfiguration configuration,
            IServiceProvider services, IHostLifetime lifetime, IOptionsMonitor<List<Product>> productOptionsMonitor)
        {
            _logger = logger;
            _logger.LogInformation($"Running DocToIdoit version {GetType().Assembly.GetName().Version}");
            _logger.LogInformation("IHostLifetime: {hostLifetime}", lifetime.GetType());
            _configuration = configuration;
            _services = services;
            _productOptionsMonitor = productOptionsMonitor;
            _timer = new Timer(new TimerCallback(CheckForFiles));
            _timer.Change(1000, _configuration.GetValue<int>("Watcher:ScanInterval"));
            _logger.LogInformation("IronOCR license is valid: " + License.IsLicensed);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }

        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Starting");
            return base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service");
            await base.StopAsync(cancellationToken);
        }

        /// <summary>
        /// Check if there are new files in the directory
        /// </summary>
        private async void CheckForFiles(object state)
        {
            //avoid checking for new files if a past operation is running
            if (_operationRunning)
                return;
            _operationRunning = true;
            _logger.LogDebug("Checking for new files");
            var newFiles = Directory.GetFiles(_configuration["Watcher:ScanPath"], "*.pdf");
            var async = _configuration.GetValue<bool>("Watcher:ProcessAsync");
            foreach (var v in newFiles)
            {
                if (async)
                    //process files in parallel/async
                    _ = ProcessFile(v);
                else
                    await ProcessFile(v);
            }
            _operationRunning = false;
        }

        /// <summary>
        /// Process a specified file.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        private async Task ProcessFile(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            _logger.LogInformation($"New file found: {fileName}");
            var sw = new Stopwatch();
            sw.Start();
            //move file to processing directory
            var path = _configuration["Watcher:ProcessingPath"] + fileName;
            try
            {
                File.Move(filePath, path, true);
            }
            catch (IOException ex)
            {
                _logger.LogError(ex, $"File {fileName} could not moved for processing");
                return;
            }
            _logger.LogDebug($"Moved file {fileName} to processing directory");
            //initialize ocr worker
            using var scope = _services.CreateScope();
            var ocrWorker = scope.ServiceProvider.GetRequiredService<IOcrWorker>();
            var result = await ocrWorker.RunOcrAsync(path);
            if (result == null)
            {
                OnServiceError(path);
                return;
            }

            var products = ExtractProducts(result);
            //check if any products were found
            if (!products.Any())
            {
                OcrToFile(Path.GetFileNameWithoutExtension(path), result.Text);
                OnServiceError(path);
                return;
            }
            //initialize idoit worker
            using var idoitWorker = scope.ServiceProvider.GetRequiredService<IIdoitWorker>();
            _logger.LogInformation($"Starting creation of idoit objects for file {fileName}");
            var failed = 0;
            //create an idoit object for each product found
            foreach (var v in products)
            {
                try
                {
                    var success = idoitWorker.CreateObject(v);
                    if (success)
                        _logger.LogInformation($"Successfully finished creation of idoit object {v.ProductName} with S/N: {v.SerialNumer}");
                }
                catch (Exception)
                {
                    _logger.LogWarning($"Failed to create idoit object {v.ProductName} with S/N: {v.SerialNumer}");
                    failed++;
                }
            }
            //if an object could not be created throw an error
            if (failed > 0)
            {
                OnServiceError(path);
                OcrToFile(Path.GetFileNameWithoutExtension(path), result.Text);
            }
            else
            {
                File.Delete(path);
                OcrToFile(Path.GetFileNameWithoutExtension(path), result.Text);
            }
            _logger.LogInformation($"Operation for file {fileName} finished in {sw.ElapsedMilliseconds}ms");
            sw.Stop();
        }

        /// <summary>
        /// Search for products in the specified OcrResult.
        /// </summary>
        /// <param name="ocr"></param>
        /// <returns>An <see cref="IList{T}"/> with all extracted products.</returns>
        internal IList<Product> ExtractProducts(OcrResult ocr)
        {
            var list = new List<Product>();
            var supportedItems = _productOptionsMonitor.CurrentValue;
            var pages = ocr.Pages;

            //loop through all pages of the file
            for (int pIndex = 0; pIndex < pages.Length; pIndex++)
            {
                //check all regex expressions
                var dateMatch = CheckMultipleRegex(_configuration.GetSection("Ocr:DateDetectionRegex").Get<IEnumerable<string>>(), ocr.Pages[pIndex].Text);
                var noteMatch = CheckMultipleRegex(_configuration.GetSection("Ocr:DeliveryNoteDetectionRegex").Get<IEnumerable<string>>(), ocr.Pages[pIndex].Text);
                var ticketIdMatch = CheckMultipleRegex(_configuration.GetSection("Ocr:TicketIdDetectionRegex").Get<IEnumerable<string>>(), ocr.Pages[pIndex].Text);

                var dateFormats = _configuration.GetSection("Ocr:DateFormats").Get<IEnumerable<string>>();
                string ticketid;
                if (!ticketIdMatch.Success)
                {
                    _logger.LogInformation($"No ticket id detected in ocr page {pIndex + 1}");
                    ticketid = string.Empty;
                }
                else
                    ticketid = ticketIdMatch.Value;
                if (!dateMatch.Success)
                {
                    _logger.LogError($"Date not detected in ocr page {pIndex + 1}");
                    continue;
                }
                var parsedDate = TryParseMultipleDateTimeFormats(dateFormats, dateMatch.Value);
                if (parsedDate == null)
                {
                    _logger.LogError($"Date failed to parse in ocr page {pIndex + 1}");
                    continue;
                }

                if (!noteMatch.Success)
                {
                    _logger.LogError($"Delivery note not detected in ocr page {pIndex + 1}");
                    continue;
                }
                var count = 0;
                var serialIndicators = _configuration.GetSection("Ocr:SerialIndicators").Get<string[]>();
                var serialDelimiter = _configuration["Ocr:SerialDelimiter"];
                //loop through each product from the configuration file
                foreach (var v in supportedItems)
                {
                    var lines = pages[pIndex].Lines;
                    //loop through all lines of the page
                    for (int lIndex = 0; lIndex < lines.Length; lIndex++)
                    {
                        //check if line contains a supported product from the list, if not go further
                        if (!pages[pIndex].Lines[lIndex].Text.ToLower().Contains(v.ProductName.ToLower()))
                            continue;
                        var serialIndex = -1;
                        string foundIndicator = null;
                        //search further lines for serial numbers
                        for (int s = lIndex + 1; s < lines.Length; s++)
                        {
                            //check if the line contains a serial indicator from the configuration
                            foreach (var indicator in serialIndicators)
                            {
                                if (lines[s].Text.Contains(indicator))
                                {
                                    serialIndex = s;
                                    foundIndicator = indicator;
                                    s = int.MaxValue - 1; //break outer loop
                                    break;
                                }
                            }
                        }
                        if (serialIndex == -1)
                            continue;
                        List<string> serials = new();
                        do
                        {
                            //serials begin after the last char of the indicator, remove spaces, new lines and delimiters
                            serials.AddRange(lines[serialIndex].Text.Substring(lines[serialIndex].Text.IndexOf(foundIndicator.Last()) + 1)
                                .Replace(" ", "")
                                .Replace("\r", "")
                                .Replace("\n", "")
                                .Split(serialDelimiter).ToList());
                            serialIndex++;
                            //if serials doesn't end in first line, use the next line too
                        } while (lines[serialIndex - 1].Text.EndsWith(serialDelimiter));
                        //when serials go through multiple lines it could happen that an empty string was added because of the line break, so we clear it
                        serials.RemoveAll((string s) => { return s == string.Empty; });
                        //loop through each serial which was found
                        foreach (var s in serials)
                        {
                            count++;
                            var p = new Product
                            {
                                OrderDate = (DateTime)parsedDate,
                                DeliveryNote = noteMatch.Value,
                                SerialNumer = s,
                                Type = v.Type,
                                IdoitPrefix = v.IdoitPrefix,
                                ProductName = v.ProductName,
                                Template = v.Template,
                                TicketId = ticketid
                            };
                            list.Add(p);
                        }
                    }
                }
                if (count == 0)
                    _logger.LogWarning($"No products found in ocr page {pIndex + 1}");
                else
                    _logger.LogDebug($"{count} products found in ocr page {pIndex + 1}");
            }
            return list;
        }

        /// <summary>
        /// Log the result of the ocr process to file.
        /// </summary>
        /// <param name="fileNameWithoutExtension"></param>
        /// <param name="text"></param>
        private async void OcrToFile(string fileNameWithoutExtension, string text)
        {
            _logger.LogDebug($"Writing ocr result to text file: {fileNameWithoutExtension + ".ocr"}");
            await File.WriteAllTextAsync(_configuration["Watcher:OcrResultPath"] + fileNameWithoutExtension + ".ocr", text);
        }

        /// <summary>
        /// Move file to error directory and send an E-Mail if configured.
        /// </summary>
        /// <param name="filePath"></param>
        private async void OnServiceError(string filePath)
        {
            var fileName = Path.GetFileName(filePath);
            if (_configuration.GetValue<bool>("Smtp:MailOnError"))
            {
                using var scope = _services.CreateScope();
                using var smtpWorker = scope.ServiceProvider.GetRequiredService<ISmtpWorker>();
                var attachments = new List<string>() { string.Format(_configuration["Logging:File:Path"], DateTime.UtcNow), filePath };
                //send email with current log file as attachment
                await smtpWorker.SendAsync($"While processing file {fileName}, errors occurred.\nCheck the log file for more information.",
                    attachments);
            }
            File.Move(filePath, _configuration["Watcher:ErrorScanPath"] + Path.GetFileName(fileName), true);
            _logger.LogInformation($"Moved file {fileName} to error directory");
        }

        internal Match CheckMultipleRegex(IEnumerable<string> regexList, string text)
        {
            foreach (var reg in regexList)
            {
                var match = new Regex(reg).Match(text);
                if (match.Success)
                {
                    return match;
                }
            }
            return Match.Empty;
        }

        internal DateTime? TryParseMultipleDateTimeFormats(IEnumerable<string> formats, string date)
        {
            foreach (var fmt in formats)
            {
                if (DateTime.TryParseExact(date, fmt, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
                {
                    return parsedDate;
                }
            }
            return null;
        }

        public override void Dispose()
        {
            _logger.LogInformation("Disposing Service");
            _timer.Dispose();
            base.Dispose();
        }
    }
}