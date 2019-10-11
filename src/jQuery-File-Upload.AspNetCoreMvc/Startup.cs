using System.IO;
using jQuery_File_Upload.AspNetCoreMvc.Infrastructure.Logging;
using jQuery_File_Upload.AspNetCoreMvc.Models;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace jQuery_File_Upload.AspNetCoreMvc
{
    public class Startup
    {
        private readonly IConfiguration _config;
        private readonly IHostEnvironment _env;

        public Startup(IConfiguration config, IHostEnvironment env)
        {
            _config = config;
            _env = env;

            // This is the earliest we can set up logging because we need to know our current environment.
            Log.Logger = ConfigureSerilog(_env);
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure MediatR.
            services.AddMediatR(typeof(Startup));

            // Application services
            services.AddScoped<FilesHelper, FilesHelper>();

            // Add framework services.
            services
                .AddMvc()
                .AddNewtonsoftJson();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostEnvironment env, ILoggerFactory loggerFactory)
        {
            // Serilog doesn't have an override that takes the Logging settings from appsettings.json, so we have to 
            //   provide our own settings here.
            loggerFactory.AddSerilog(Log.Logger);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseRouting();

            app.UseEndpoints(routes =>
            {
                routes.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
            });
        }


        //
        // Private methods
        //

        /// <summary>
        /// <para>Serilog is our application logger. Default to Verbose. If we need to control this dynamically at some point
        /// in the future, we can: https://nblumhardt.com/2014/10/dynamically-changing-the-serilog-level/ </para>
        /// </summary>
        private Serilog.Core.Logger ConfigureSerilog(IHostEnvironment env)
        {
            var loggerConfig = new LoggerConfiguration();

            // Set the verbosity: Information and higher in production; verbose everywhere else.
            loggerConfig = env.IsProduction()
                ? loggerConfig.MinimumLevel.Information()
                : loggerConfig.MinimumLevel.Verbose();

            return loggerConfig
                .Enrich.With<UtcTimestampEnricher>()
                .WriteTo.File(Path.Combine(env.ContentRootPath, "Logs", "log.txt"), outputTemplate: "{UtcTimestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] [{SourceContext:l}] {Message}{NewLine}{Exception}", rollingInterval: RollingInterval.Day)
                .CreateLogger();
        }
    }
}
