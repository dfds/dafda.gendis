using System.Diagnostics;
using Dapper;

namespace Dafda.Gendis.App;

public class QueueProcessor
{
    private readonly ILogger<QueueProcessor> _logger;
    private readonly IDbConnectionProvider _connectionProvider;
    private readonly IProducer _producer;
    private readonly SystemTime _systemTime;

    public QueueProcessor(ILogger<QueueProcessor> logger, IDbConnectionProvider connectionProvider, IProducer producer, SystemTime systemTime)
    {
        _logger = logger;
        _connectionProvider = connectionProvider;
        _producer = producer;
        _systemTime = systemTime;
    }

    public async Task ProcessQueue(CancellationToken cancellationToken)
    {
        var conn = _connectionProvider.Get();
        var entries = (await conn.QueryAsync<OutboxEntry>("select * from \"_outbox\" where \"ProcessedUtc\" is null order by \"OccurredUtc\"")).ToArray();

        if (entries.Length == 0)
        {
            return;
        }

        using var _ =_logger.BeginScope("Batch id: {QueueBatchId}, Batch size: {QueueBatchSize}", Guid.NewGuid().ToString("N"), entries.Length);

        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Aborting processing batch - cancellation has been requested!");
                break;
            }

            var stopwatch = Stopwatch.StartNew();

            await _producer.Produce(
                topic: entry.Topic,
                partitionKey: entry.Key,
                outboxItemId: entry.Id.ToString("N"),
                message: entry.Payload
            );

            await conn.ExecuteAsync($"update \"_outbox\" set \"ProcessedUtc\" = '{_systemTime.UtcNow}' where \"Id\" = '{entry.Id}'");

            _logger.LogDebug("Processed outbox item #{OutboxItemId} in [{ElapsedTime}]", entry.Id.ToString("N"), stopwatch.Elapsed);
        }
    }
}