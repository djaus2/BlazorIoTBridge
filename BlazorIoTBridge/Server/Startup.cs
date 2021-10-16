using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Linq;

using BlazorIoTBridge.Shared;
using System;

namespace BlazorIoTBridge.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
   
            //      var config = new ConfigurationBuilder()
            //.SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            //.AddJsonFile("appsettings.json").Build();
            //      //Settings mySettingsConfig = new Settings();
            //      //configuration.GetSection("AppSettings").Bind(mySettingsConfig);

            //      //var section = configuration.GetSection("AppSettings");
            //      //var set = section.Get<AppSettings>();

            //      var settings3 = config.GetSection("AppSettings");//.Get<Settings>();
            //     // Shared.AppSettings.settings = mySettingsConfig;
            //      //.evIOTHUB_DEVICE_CONN_STRING = mySettingsConfig.IOTHUB_DEVICE_CONN_STRING;
            //      //Shared.AppSettings.settings.IOTHUB_DEVICE_CONN_STRING = mySettingsConfig.IOTHUB_DEVICE_CONN_STRING;
        }

   

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllersWithViews();
            services.AddRazorPages();

            services.AddSingleton<IConfiguration>(Configuration);

            //services.AddSingleton (IAppSettings,AppSettings)

            //services.AddSingleton(Configuration.GetSection("Setx").Get<Setx>());

            /*var section = Configuration.GetSection(nameof(Setx));
            var appSettings = section.Get<Setx>();*/
                     var config = new ConfigurationBuilder()
      .AddJsonFile("appsettings.json").Build();

            var _appSettings = config.GetSection("AppSettings").Get<AppSettings>();

            services.AddSingleton(_appSettings);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
