using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace WatchTemplate
{
    public class Program
    {
        public static string OriginalFolder;
        public static string ChangedFolder;

        public static void Main(string[] args)
        {
            var builder = new CommandLineBuilder()
                .AddOption(new Option<string>("--folder"))
                .AddOption(new Option<string>("--compare"))
                .Build();
            var p = builder.Parse(args);
            OriginalFolder = p.ValueForOption<string>("--folder") ?? Directory.GetCurrentDirectory();
            ChangedFolder = p.ValueForOption<string>("--compare");

            var currentPath = Path.GetDirectoryName(typeof(Program).Assembly.Location);
            while (!Directory.Exists(Path.Combine(currentPath, "wwwroot")))
            {
                currentPath = Path.GetDirectoryName(currentPath);
                if (currentPath == null)
                {
                    throw new Exception("Couldn't find wwwroot folder.");
                }
            }
            Directory.SetCurrentDirectory(currentPath);

            CreateHostBuilder(Array.Empty<string>()).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
