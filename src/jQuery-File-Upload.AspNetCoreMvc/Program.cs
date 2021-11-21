using jQuery_File_Upload.AspNetCoreMvc.Infrastructure;
using jQuery_File_Upload.AspNetCoreMvc.Models;
using MediatR;
using Serilog;

// The initial "bootstrap" logger is able to log errors during start-up. It's completely replaced by the
//   logger configured in `UseSerilog()`, once configuration and dependency-injection have both been
//   set up successfully.

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();


//
// Create the builder.
//

var builder = WebApplication.CreateBuilder(args);


//
// Add services to the container.
//

builder.Host.UseSerilog(WebHostBuilderHelper.ConfigureSerilog);

// Configure MediatR.
builder.Services.AddMediatR(typeof(Program));

// Application services
builder.Services.AddScoped<FilesHelper, FilesHelper>();

// Add framework services.
builder.Services
    .AddMvc()
    .AddNewtonsoftJson();

try
{
    //
    // Build the application and configure the HTTP request pipeline.
    //

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
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

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, $"Unhandled exception in {nameof(Program)}: {ex}");

    throw;
}
finally
{
    Log.CloseAndFlush();
}
