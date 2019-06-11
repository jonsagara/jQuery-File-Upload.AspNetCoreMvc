using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;

namespace jQuery_File_Upload.AspNetCoreMvc
{
    public class Program
    {
        public static void Main(string[] args)
        {
            // As of 2.0, it's bad practice to do anything in BuildWebHost except build and configure the web host. 
            //   Anything that is about running the application should be handled outside of BuildWebHost — typically
            //   in the Main method of Program.cs.
            //   See: https://docs.microsoft.com/en-us/aspnet/core/migration/1x-to-2x/#move-database-initialization-code
            var host = CreateWebHostBuilder(args)
                .Build();

            host.Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
