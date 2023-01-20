using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using MailKit.Net.Smtp;
using MailKit;
using MimeKit;
using System.Threading.Tasks;
using System.Collections.Generic;

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
            _smtpClient = new SmtpClient();
            _smtpClient.MessageSent += _smtpClient_MessageSent;
            _logger.LogDebug("SmtpWorker initialized");
        }

        private void _smtpClient_MessageSent(object sender, MessageSentEventArgs e)
        {
            _logger.LogDebug($"Mail sent successfully. Server response: {e.Response}");
        }

        public async Task SendAsync(string body, IEnumerable<string> attachments = null)
        {
            var message = new MimeMessage();
            message.Subject = _configuration["Smtp:Subject"];
            message.From.Add(new MailboxAddress("", _configuration["Smtp:From"]));
            message.To.Add(new MailboxAddress("", _configuration["Smtp:To"]));
            var builder = new BodyBuilder();
            builder.TextBody = body;
            if (attachments != null)
            {
                foreach (var a in attachments)
                {
                    try
                    {
                        await builder.Attachments.AddAsync(a);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to add message attachment {a}");
                    }
                }
            }
            message.Body = builder.ToMessageBody();
            try
            {
                if (_configuration["Smtp:Username"] != string.Empty && _configuration["Smtp:Password"] != string.Empty)
                    await _smtpClient.AuthenticateAsync(_configuration["Smtp:Username"], _configuration["Smtp:Password"]);
                await _smtpClient.ConnectAsync(_configuration["Smtp:Server"],
                                                    _configuration.GetValue<int>("Smtp:Port"));
                await _smtpClient.SendAsync(message);
                await _smtpClient.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send e-mail");
            }
        }

        public void Dispose()
        {
            _smtpClient.MessageSent -= _smtpClient_MessageSent;
            _smtpClient.Dispose();
        }
    }
}