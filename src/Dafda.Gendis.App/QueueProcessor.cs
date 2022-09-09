using Dapper;

namespace Dafda.Gendis.App;

public class QueueProcessor
{
    private readonly ILogger<QueueProcessor> _logger;
    private readonly DbConnectionFactory _connectionFactory;
    private readonly IProducer _producer;

    public QueueProcessor(ILogger<QueueProcessor> logger, DbConnectionFactory connectionFactory, IProducer producer)
    {
        _logger = logger;
        _connectionFactory = connectionFactory;
        _producer = producer;
    }

    public void ProcessQueue(CancellationToken cancellationToken)
    {
        using var conn = _connectionFactory.CreateOpenConnection();

        var entries = conn
            .Query<OutboxEntry>("select * from \"_outbox\" where \"ProcessedUtc\" is null order by \"OccurredUtc\"")
            .ToArray();

        _logger.LogDebug($"items to process: {entries.Length}");
        Console.WriteLine("lala");

        foreach (var entry in entries)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            // send message to kafka

            conn.Execute($"update \"_outbox\" set \"ProcessedUtc\" = '{DateTime.UtcNow}' where \"Id\" = '{entry.Id}'");
            _logger.LogDebug($"processed: #{entry.Id} - with payload: {entry.Payload}");
        }
    }
}