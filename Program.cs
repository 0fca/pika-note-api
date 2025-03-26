using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PikaNoteAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder = webBuilder
                        .ConfigureKestrel((context, options) =>
                        {
                            options.Limits.MaxRequestBodySize = 268435456;
                        });
                        if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development") {
                            webBuilder = webBuilder.UseUrls($"http://note.cloud.localhost:{args[0]}", $"https://note.cloud.localhost:{int.Parse(args[0]) + 1}");
                        } else
                        {
                            webBuilder = webBuilder.UseUrls($"http://note.cloud.localhost");
                        }

                    webBuilder.UseKestrel();
                });
    }
}
