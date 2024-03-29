﻿using IronOcr;
using IronOcr.Events;
using System;
using System.Threading.Tasks;

namespace DocToIdoit
{
    /// <summary>
    /// Defines functions to process files with ocr.
    /// </summary>
    internal interface IOcrWorker
    {
        /// <summary>
        /// Progress of the OCR process.
        /// </summary>
        event EventHandler<OcrProgressEventsArgs> ProgressChanged;

        /// <summary>
        /// Start OCR process of a file.
        /// </summary>
        /// <param name="path">Path to the file which should be processed.</param>
        /// <returns>An <see cref="OcrResult"/></returns>
        Task<OcrResult> RunOcrAsync(string path);
    }
}