using IronOcr;
using IronOcr.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
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
            var languageFile = _configuration.GetValue<string>("Ocr:CustomLanguageFile");
            if (languageFile == string.Empty)
                _ocrEngine.Language = OcrLanguage.GermanBest;
            else
            {
                _logger.LogInformation($"Setting custom language file to {languageFile}");
                _ocrEngine.UseCustomTesseractLanguageFile(languageFile);
            }
            _ocrEngine.Configuration.ReadBarCodes = false;
            _ocrEngine.Configuration.RenderSearchablePdfsAndHocr = false;
            _logger.LogDebug("OcrWorker initialized");
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
                input.TargetDPI = _configuration.GetValue<int>("Ocr:Scale");
                _ocrEngine.OcrProgress += ProgressChanged;
                result = await _ocrEngine.ReadAsync(input);
                _ocrEngine.OcrProgress -= ProgressChanged;
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