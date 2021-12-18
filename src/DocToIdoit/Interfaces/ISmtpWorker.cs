using System;
using System.Net.Mail;
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
        /// <param name="body"></param>
        /// <param name="attachment"></param>
        /// <param name="attachment2"></param>
        /// <returns></returns>
        Task SendAsync(string body, Attachment attachment = null, Attachment attachment2 = null);
    }
}