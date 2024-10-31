using System.Linq;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace PikaNoteAPI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0 || string.IsNullOrEmpty(args[0]))
            {
                var tmp = args.ToList();
                tmp.Add("9000");
                args = tmp.ToArray();
            }
            CreateHostBuilder(args)
                .Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder
                        .ConfigureKestrel((context, options) =>
                        {
                            options.Limits.MaxRequestBodySize = 268435456;
                        })
                        .UseUrls($"http://note.cloud.localhost:{args[0]}", $"https://note.cloud.localhost:{int.Parse(args[0]) + 1}")
                        .UseKestrel();
                });
    }
}