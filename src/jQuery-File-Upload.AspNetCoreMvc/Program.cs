using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;

namespace jQuery_File_Upload.AspNetCoreMvc
{
    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                CreateHostBuilder(args).Build().Run();

                return 0;
            }
            catch (Exception ex)
            {
                // Log the exception and let the application terminate. Try to write to Serilog, but in case it's not
                //   yet configured, also write out to the console.
                Console.Error.WriteLine($"Host terminated unexpectedly: {ex}");
                Log.Fatal(ex, "Host terminated unexpectedly");

                return 1;
            }
            finally
            {
                // Ensure all logs are written before the application terminates.
                Log.CloseAndFlush();
            }
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(
                    webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
    }
}
