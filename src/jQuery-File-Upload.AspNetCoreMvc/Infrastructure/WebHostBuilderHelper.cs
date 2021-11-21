using jQuery_File_Upload.AspNetCoreMvc.Infrastructure.Logging;
using Serilog;

namespace jQuery_File_Upload.AspNetCoreMvc.Infrastructure;

public static class WebHostBuilderHelper
{
    public static void ConfigureSerilog(HostBuilderContext context, IServiceProvider services, LoggerConfiguration loggerConfig)
    {
        //
        // Set up the rolling file directory in a "Logs" directory off of the content root.
        //

        var logDirectory = Path.Combine(context.HostingEnvironment.ContentRootPath, "Logs");


        //
        // Configure Serilog and its various sinks.
        //

        // Write to a rolling file.
        loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.With<UtcTimestampEnricher>()
            .Enrich.WithMachineName()
            .WriteTo.File(Path.Combine(logDirectory, "log.txt"), outputTemplate: "{UtcTimestamp:yyyy-MM-dd HH:mm:ss.fff} [{MachineName}] [{Level}] [{SourceContext:l}] {Message}{NewLine}{Exception}", rollingInterval: RollingInterval.Day, retainedFileCountLimit: 10);
    }
}
