using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DocToIdoit
{
    /// <summary>
    /// Defines functions to use SMTP.
    /// </summary>
    internal interface ISmtpWorker : IDisposable
    {
        /// <summary>
        /// Send an E-mail.
        /// </summary>
        /// <param name="body">Message body</param>
        /// <param name="attachments">Paths to files which should be attached</param>
        /// <returns></returns>
        Task SendAsync(string body, IEnumerable<string> attachments);
    }
}