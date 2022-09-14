using Dafda.Gendis.App;
using Dafda.Gendis.App.Configuration;
using Prometheus;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateBootstrapLogger();

Log.Information("Starting up");

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

builder.ConfigureSerilog();
builder.ConfigureDatabase();
builder.ConfigureKafkaProducer();

builder.Services.AddSingleton<SystemTime>(_ => SystemTime.CreateDefault());
builder.Services.AddSingleton(_ =>
{
    var channelName = builder.Configuration["DAFDA_OUTBOX_NOTIFICATION_CHANNEL"] ?? "dafda_outbox";
    return new DafdaNotificationChannelSettings(channelName);
});

builder.Services.AddSingleton(_ => new AutoResetEvent(true));
builder.Services.AddTransient<QueueProcessor>();
builder.Services.AddHostedService<OutboxChangeListener>();
builder.Services.AddHostedService<MessageDispatcher>();

var app = builder.Build();

app.UseRouting();
app.UseEndpoints(x =>
{
    x.MapMetrics();
});

app.Run();