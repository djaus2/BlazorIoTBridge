using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using System.IO;
using System.Diagnostics;

namespace BlazorSensorAppNet5.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {

            /*string app = @"C:\Users\DavidJones\source\repos\BlazorSensorAppNet5\BlazorSensorAppNet5\Serail2Blazor\bin\Debug\net5.0\Serail2Blazor.exe";
            string svr = AppDomain.CurrentDomain.BaseDirectory;
            var path = Path.Combine(svr ,app);
            Process.Start(new ProcessStartInfo(app));*/
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostingContext, config) =>
                {
                    config.AddEnvironmentVariables();
                    config.SetBasePath(Directory.GetCurrentDirectory());
                    config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
