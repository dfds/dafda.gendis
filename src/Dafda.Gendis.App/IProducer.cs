namespace Dafda.Gendis.App;

public interface IProducer
{
    Task Produce(string topic, string partitionKey, string outboxItemId, string message);
}