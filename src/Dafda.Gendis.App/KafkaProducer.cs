using Confluent.Kafka;

namespace Dafda.Gendis.App;

public class KafkaProducer : IProducer
{
    private readonly ILogger<KafkaProducer> _logger;
    private readonly IProducer<string, string> _innerProducer;

    public KafkaProducer(ILogger<KafkaProducer> logger, IProducer<string, string> innerProducer)
    {
        _logger = logger;
        _innerProducer = innerProducer;
    }

    public async Task Produce(string topic, string partitionKey, string outboxItemId, string message)
    {
        var result = await _innerProducer.ProduceAsync(
            topic: topic,
            message: new Message<string, string>
            {
                Key = partitionKey,
                Value = message
            }
        );

        if (result.Status != PersistenceStatus.Persisted)
        {
            throw new Exception($"Message not delivered properly to Kafka. Status was \"{result.Status:G}\".");
        }

        _logger.LogTrace("Dispatched outbox item #{OutboxItemId} to Kafka topic \"{Topic}\" with partition key \"{PartitionKey}\". Status: {Status}", outboxItemId, topic, partitionKey, result.Status.ToString("G"));
    }
}