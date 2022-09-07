using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Sinks.SystemConsole.Themes;

namespace Dafda.Gendis.App.Configuration;

public static class Serilog
{
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, configuration) =>
        {
            configuration
                .Enrich.FromLogContext()
                .Enrich.WithProperty("Application", "Dafda.Gendis")
                .Enrich.WithProperty("Environment", context.Configuration["Environment"])
                .MinimumLevel.Verbose()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Extensions.Diagnostics.HealthChecks", LogEventLevel.Warning)
                .MinimumLevel.Override("Dafda", LogEventLevel.Warning);

            configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code);

            //if (context.HostingEnvironment.IsProduction())
            //{
            //    configuration.WriteTo.Console(new CompactJsonFormatter());
            //}
            //else
            //{
            //    configuration.WriteTo.Console(theme: AnsiConsoleTheme.Code);
            //}
        });
    }
}