using Dapper;
using Npgsql;

namespace Dafda.Gendis.App;

public class OutboxChangeListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly DafdaNotificationChannelSettings _options;

    public OutboxChangeListener(IServiceProvider serviceProvider, DafdaNotificationChannelSettings options)
    {
        _serviceProvider = serviceProvider;
        _options = options;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var conn = scope.ServiceProvider.GetRequiredService<NpgsqlConnection>();
            var resetEvent = scope.ServiceProvider.GetRequiredService<AutoResetEvent>();
            conn.Notification += (_, _) => resetEvent.Set();
            
            await conn.OpenAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                await conn.ExecuteAsync($"LISTEN {_options.Name}", commandTimeout: 10);
                await conn.WaitAsync(stoppingToken);
            }
        }, stoppingToken);
    }
}