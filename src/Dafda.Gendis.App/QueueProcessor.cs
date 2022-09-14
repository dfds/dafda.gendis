using System.Data;
using System.Diagnostics;
using Dapper;
using Prometheus;

namespace Dafda.Gendis.App;

public class QueueProcessor
{
    private static readonly Histogram ProcessingDurations = Metrics.CreateHistogram(
        name: "outbox_entry_processing_duration_seconds",
        help: "Measures the total time that it takes to process a single outbox item (dispatching + marking as processed)"
    );

    private static readonly Gauge OutboxSize = Metrics.CreateGauge(
        name: "outbox_size",
        help: "Indicates how many items that are unprocessed in the outbox",
        configuration: new GaugeConfiguration
        {
            SuppressInitialValue = false
        }
    );

    private static readonly Counter DispatchedToKafka = Metrics.CreateCounter(
        name: "outbox_entries_dispatched_to_kafka_total",
        help: "Counts how many outbox entries that has been dispatched to kafka",
        configuration: new CounterConfiguration { SuppressInitialValue = false }
    );

    private static readonly Counter MarkedAsProcessed = Metrics.CreateCounter(
        name: "outbox_entries_marked_as_processed_total",
        help: "Counts how many outbox entries that has been finalized and marked as processed",
        configuration: new CounterConfiguration { SuppressInitialValue = false }
    );

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

        OutboxSize.Set(entries.Length);

        if (entries.Length == 0)
        {
            return;
        }

        _logger.LogTrace("Batch size: {QueueBatchSize}", entries.Length);

        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.LogWarning("Abort processing batch - cancellation has been requested!");
                break;
            }

            await ProcessSingle(entry, conn);
            OutboxSize.Dec();
        }
    }

    private async Task ProcessSingle(OutboxEntry entry, IDbConnection conn)
    {
        using var timer = ProcessingDurations.NewTimer();
        var stopwatch = Stopwatch.StartNew();

        await _producer.Produce(
            topic: entry.Topic,
            partitionKey: entry.Key,
            outboxItemId: entry.Id.ToString("N"),
            message: entry.Payload
        );
        DispatchedToKafka.Inc();

        await conn.ExecuteAsync($"update \"_outbox\" set \"ProcessedUtc\" = '{_systemTime.UtcNow:s}' where \"Id\" = '{entry.Id}'");
        MarkedAsProcessed.Inc();

        _logger.LogDebug("Processed outbox item #{OutboxItemId} in [{ElapsedTime}]", entry.Id.ToString("N"), stopwatch.Elapsed);
    }
}