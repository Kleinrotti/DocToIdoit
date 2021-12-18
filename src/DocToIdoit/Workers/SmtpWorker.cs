using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace DocToIdoit
{
    /// <summary>
    /// Provides functions to use SMTP.
    /// </summary>
    internal class SmtpWorker : ISmtpWorker
    {
        private readonly ILogger<ISmtpWorker> _logger;
        private readonly IConfiguration _configuration;
        private readonly SmtpClient _smtpClient;

        public SmtpWorker(ILogger<ISmtpWorker> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
            _smtpClient = new SmtpClient
            {
                Host = _configuration["Smtp:Server"],
                Port = _configuration.GetValue<int>("Smtp:Port")
            };
            _logger.LogDebug("SmtpWorker initialized");
        }

        public async Task SendAsync(string body, Attachment attachment1 = null, Attachment attachment2 = null)
        {
            using var message = new MailMessage(_configuration["Smtp:From"], _configuration["Smtp:To"])
            {
                Subject = _configuration["Smtp:Subject"],
                Body = body,
            };
            message.Attachments.Add(attachment1);
            message.Attachments.Add(attachment2);
            try
            {
                await _smtpClient.SendMailAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send E-mail");
            }
        }

        public void Dispose()
        {
            _smtpClient.Dispose();
        }
    }
}