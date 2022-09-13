using System.Collections.Generic;
using System.Threading.Tasks;
using Dafda.Gendis.App;

namespace Dafda.Gendis.Tests.TestDoubles;

public class SpyProducer : IProducer
{
    public List<ProducedMessage> SendMessages { get; } = new();
    public bool WasProduceInvoked { get; private set; }

    public Task Produce(string topic, string partitionKey, string outboxItemId, string message)
    {
        WasProduceInvoked = true;
        SendMessages.Add(new ProducedMessage(topic, partitionKey, outboxItemId, message));

        return Task.CompletedTask;
    }

    public record ProducedMessage(string? Topic, string? PartitionKey, string? OutboxItemId, string? Message);
}