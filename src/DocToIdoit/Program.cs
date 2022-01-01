using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;

namespace DocToIdoit
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSystemd()
                .UseWindowsService()
                .ConfigureServices((hostContext, services) =>
                {
                    services.AddHostedService<ServiceWorker>();
                    services.AddScoped<IOcrWorker, OcrWorker>();
                    services.AddScoped<IIdoitWorker, IdoitWorker>();
                    services.AddScoped<ISmtpWorker, SmtpWorker>();
                    services.Configure<List<Product>>(hostContext.Configuration.GetSection("Ocr:SupportedProducts"));
                    services.AddLogging(loggingBuilder =>
                    {
                        var loggingSection = hostContext.Configuration.GetSection("Logging");
                        loggingBuilder.AddFile(loggingSection, fileLoggerOpts =>
                        {
                            fileLoggerOpts.FormatLogFileName = fName =>
                            {
                                return string.Format(fName, DateTime.UtcNow);
                            };
                        });
                    });
                });
    }
}