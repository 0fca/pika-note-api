using System;
using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;


namespace PikaNoteAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args) 
                .Build()
                .Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder
                    .UseConfiguration(new ConfigurationBuilder()
                    .AddEnvironmentVariables()
                    .AddCommandLine(args).Build())
                    .UseKestrel();
                    webBuilder = webBuilder
                        .ConfigureKestrel((context, options) =>
                        {
                            options.Limits.MaxRequestBodySize = 268435456;
                            var urls = context.Configuration["Kestrel:Urls"] ?? "http://note.cloud.localhost";
                            var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";                            
                            webBuilder.UseUrls(urls.Split(';'));
                        });
                });
    }
}