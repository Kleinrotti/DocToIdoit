using IronOcr;
using IronOcr.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;

namespace DocToIdoit
{
    /// <summary>
    /// Perform ocr actions on files.
    /// </summary>
    internal class OcrWorker : IOcrWorker
    {
        private readonly ILogger<OcrWorker> _logger;
        private IronTesseract _ocrEngine;
        private readonly IConfiguration _configuration;
        public event EventHandler<OcrProgresEventsArgs> ProgressChanged;

        public OcrWorker(ILogger<OcrWorker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _ocrEngine = new IronTesseract();
            _ocrEngine.OcrProgress += ProgressChanged;
            _ocrEngine.Language = OcrLanguage.GermanBest;
            _ocrEngine.Configuration.TesseractVersion = TesseractVersion.Tesseract5;
            //AI OCR only without font analysis
            _ocrEngine.Configuration.EngineMode = TesseractEngineMode.LstmOnly;
            _ocrEngine.Configuration.ReadBarCodes = false;
            _ocrEngine.Configuration.RenderSearchablePdfsAndHocr = false;
            _ocrEngine.Configuration.PageSegmentationMode = TesseractPageSegmentationMode.SingleColumn;
            _logger.LogDebug("OcrWorker initialized");
        }

        public async Task<IEnumerable<OcrResult>> RunOcrAsync(string path, Rectangle[] zones)
        {
            _logger.LogInformation($"Starting Ocr of file {path}");
            var result = new List<OcrResult>();
            try
            {
                for (int i = 0; i < zones.Length; i++)
                {
                    using var input = new OcrInput();
                    //input.AddPdf(path, null, zones[i], _configuration.GetValue<int>("Ocr:ScanDpi"));
                    input.AddPdf(path, null);
                    if (_configuration.GetValue<bool>("Ocr:Deskew"))
                        input.Deskew();
                    input.EnhanceResolution(_configuration.GetValue<int>("Ocr:Scale"));
                    var ocr = await _ocrEngine.ReadAsync(input);
                    _logger.LogDebug($"Finished processing OCR zone {i} of file {path}. Confidence: {(int)ocr.Confidence}%");
                    result.Add(ocr);
                }
                _logger.LogInformation($"Finished Ocr of file {path}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ocr of file {path} failed");
            }
            return result;
        }

        public async Task<OcrResult> RunOcrAsync(string path)
        {
            _logger.LogInformation($"Starting Ocr of file {path}");
            OcrResult result = null;
            try
            {
                using var input = new OcrInput();
                input.AddPdf(path, null);
                if (_configuration.GetValue<bool>("Ocr:Deskew"))
                    input.Deskew();
                input.EnhanceResolution(_configuration.GetValue<int>("Ocr:Scale"));
                result = await _ocrEngine.ReadAsync(input);
                _logger.LogInformation($"Finished Ocr of file {path}. Confidence: {(int)result.Confidence}%");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Ocr of file {path} failed");
            }
            return result;
        }
    }
}