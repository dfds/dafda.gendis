namespace Dafda.Gendis.App;

public interface IProducer
{
    void Produce(string topic, string partitionKey, string message);
}