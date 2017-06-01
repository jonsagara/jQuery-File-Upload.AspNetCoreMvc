using System.IO;
using jQuery_File_Upload.AspNetCoreMvc.Infrastructure.Logging;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;

namespace jQuery_File_Upload.AspNetCoreMvc
{
    public class Startup
    {
        public Startup(IHostingEnvironment env)
        {
            // Serilog is our application logger. Default to Verbose. If we need to control this dynamically at some point
            //   in the future, we can: https://nblumhardt.com/2014/10/dynamically-changing-the-serilog-level/
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.With<UtcTimestampEnricher>()
                .WriteTo.RollingFile(Path.Combine(env.ContentRootPath, "Logs", "log-{Date}.txt"), outputTemplate: "{UtcTimestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] [{SourceContext:l}] {Message}{NewLine}{Exception}")
                .CreateLogger();

            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            Configuration = builder.Build();
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Configure MediatR.
            services.AddMediatR(typeof(Startup));

            // Add framework services.
            services
                .AddMvc()
                .AddRazorOptions(opt =>
                {
                    // Enables use of C# 7 language features in views.
                    opt.ParseOptions = opt.ParseOptions.WithLanguageVersion(LanguageVersion.CSharp7);
                });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Serilog doesn't have an override that takes the Logging settings from appsettings.json, so we have to 
            //   provide our own settings here.
            loggerFactory.WithFilter(new FilterLoggerSettings
                {
                    { "Microsoft", LogLevel.Warning },
                    { "System", LogLevel.Warning },
                    { "Default", LogLevel.Trace }
                })
                .AddSerilog(Log.Logger);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBrowserLink();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
