namespace Dafda.Gendis.App;

public class MessageDispatcher : BackgroundService
{
    private readonly ILogger<MessageDispatcher> _logger;
    private readonly IServiceProvider _serviceProvider;

    public MessageDispatcher(ILogger<MessageDispatcher> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
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

                using var _ = _logger.BeginScope("Batch id: {QueueBatchId}", Guid.NewGuid().ToString("N"));

                try
                {
                    await processor.ProcessQueue(stoppingToken);
                }
                catch (Exception err)
                {
                    _logger.LogError(err, "Fatal error while processing outbox!");
                }

                resetEvent.WaitOne(TimeSpan.FromSeconds(10));
            }
        }, stoppingToken);
    }
}