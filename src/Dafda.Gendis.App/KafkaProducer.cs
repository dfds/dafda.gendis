using Confluent.Kafka;

namespace Dafda.Gendis.App;

public class KafkaProducer : IProducer
{
    private readonly IProducer<string, string> _innerProducer;

    public KafkaProducer(IProducer<string, string> innerProducer)
    {
        _innerProducer = innerProducer;
    }

    public void Produce(string topic, string partitionKey, string message)
    {
        var result = _innerProducer.ProduceAsync(
                topic: topic,
                message: new Message<string, string>
                {
                    Key = partitionKey,
                    Value = message
                }
            )
            .ConfigureAwait(false)
            .GetAwaiter()
            .GetResult();

        //result.Status == PersistenceStatus.Persisted
    }
}