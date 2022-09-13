namespace Dafda.Gendis.App;

public class MessageDispatcher : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public MessageDispatcher(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        return Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _serviceProvider.CreateScope();
                var processor = scope.ServiceProvider.GetRequiredService<QueueProcessor>();
                var resetEvent = scope.ServiceProvider.GetRequiredService<AutoResetEvent>();
                
                await processor.ProcessQueue(stoppingToken);

                resetEvent.WaitOne(TimeSpan.FromSeconds(10));
            }
        }, stoppingToken);
    }
}