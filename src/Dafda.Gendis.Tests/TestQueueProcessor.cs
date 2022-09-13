using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dafda.Gendis.App;
using Dafda.Gendis.Tests.Builders;
using Dafda.Gendis.Tests.TestDoubles;
using Dapper;
using Xunit;

namespace Dafda.Gendis.Tests;

public class TestQueueProcessor
{
    [Fact]
    public async Task when_no_outbox_entries_exists_no_message_is_sent()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var connection = await databaseFactory.CreateDbConnection();

        var spy = new SpyProducer();

        var sut = new QueueProcessorBuilder()
            .WithConnectionProvider(new StubDbConnectionProvider(connection))
            .WithProducer(spy)
            .Build();

        await sut.ProcessQueue(CancellationToken.None);

        Assert.False(spy.WasProduceInvoked);
    }

    [Fact]
    public async Task when_no_pending_outbox_entries_exists_no_message_is_sent()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var connection = await databaseFactory.CreateDbConnection();

        var spy = new SpyProducer();

        await connection.ExecuteAsync($@"
            insert into _outbox (Id, Topic, Key, Payload, OccurredUtc, ProcessedUtc)
            values('{Guid.NewGuid():N}', 'foo', 'bar', '1', '2000-01-01', '2000-01-01');
        ");

        var sut = new QueueProcessorBuilder()
            .WithConnectionProvider(new StubDbConnectionProvider(connection))
            .WithProducer(spy)
            .Build();

        await sut.ProcessQueue(CancellationToken.None);

        Assert.False(spy.WasProduceInvoked);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    [InlineData("baz")]
    [InlineData("qux")]
    public async Task produces_message_on_expected_topic(string expectedTopic)
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var connection = await databaseFactory.CreateDbConnection();

        await connection.ExecuteAsync($@"
            insert into _outbox (Id, Topic, Key, Payload, OccurredUtc)
            values('00000000-0000-0000-0000-000000000000', '{expectedTopic}', 'dummy', '1', '2000-01-01');
        ");

        var spy = new SpyProducer();

        var sut = new QueueProcessorBuilder()
            .WithConnectionProvider(new StubDbConnectionProvider(connection))
            .WithProducer(spy)
            .Build();

        await sut.ProcessQueue(CancellationToken.None);

        Assert.Equal(expectedTopic, spy.SendMessages.Single().Topic);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    [InlineData("baz")]
    [InlineData("qux")]
    public async Task produces_message_with_expected_partition_key(string expectedKey)
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var connection = await databaseFactory.CreateDbConnection();

        await connection.ExecuteAsync($@"
            insert into _outbox (Id, Topic, Key, Payload, OccurredUtc)
            values('00000000-0000-0000-0000-000000000000', 'dummy', '{expectedKey}', '1', '2000-01-01');
        ");

        var spy = new SpyProducer();

        var sut = new QueueProcessorBuilder()
            .WithConnectionProvider(new StubDbConnectionProvider(connection))
            .WithProducer(spy)
            .Build();

        await sut.ProcessQueue(CancellationToken.None);

        Assert.Equal(expectedKey, spy.SendMessages.Single().PartitionKey);
    }

    [Theory]
    [InlineData("foo")]
    [InlineData("bar")]
    [InlineData("baz")]
    [InlineData("qux")]
    public async Task produces_message_with_expected_payload(string expected)
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var connection = await databaseFactory.CreateDbConnection();

        await connection.ExecuteAsync($@"
            insert into _outbox (Id, Topic, Key, Payload, OccurredUtc)
            values('00000000-0000-0000-0000-000000000000', 'dummy', 'dummy', '{expected}', '2000-01-01');
        ");

        var spy = new SpyProducer();

        var sut = new QueueProcessorBuilder()
            .WithConnectionProvider(new StubDbConnectionProvider(connection))
            .WithProducer(spy)
            .Build();

        await sut.ProcessQueue(CancellationToken.None);

        Assert.Equal(expected, spy.SendMessages.Single().Message);
    }

    [Fact]
    public async Task assigns_expected_processed_time_when_done()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var connection = await databaseFactory.CreateDbConnection();

        await connection.ExecuteAsync($@"
            insert into _outbox (Id, Topic, Key, Payload, OccurredUtc)
            values('00000000-0000-0000-0000-000000000000', 'dummy', 'dummy', 'dummy', '2000-01-01');
        ");

        var expected = new DateTime(2000, 1, 1, 1, 1, 1, DateTimeKind.Utc);

        var sut = new QueueProcessorBuilder()
            .WithConnectionProvider(new StubDbConnectionProvider(connection))
            .WithSystemTime(expected)
            .Build();

        await sut.ProcessQueue(CancellationToken.None);

        var stored = await connection.QuerySingleAsync<OutboxEntry>($"select * from _outbox");

        Assert.Equal(expected, stored.ProcessedUtc);
    }

    [Fact]
    public async Task produces_messages_for_all_pending_outbox_entries()
    {
        await using var databaseFactory = new InMemoryDatabaseFactory();
        var connection = await databaseFactory.CreateDbConnection();

        var expectedMessagePayloads = new[] {"foo", "bar", "baz", "qux"};

        foreach (var payload in expectedMessagePayloads)
        {
            await connection.ExecuteAsync($@"
                insert into _outbox (Id, Topic, Key, Payload, OccurredUtc)
                values('{Guid.NewGuid():N}', 'dummy', 'dummy', '{payload}', '2000-01-01');
            ");
        }

        var sut = new QueueProcessorBuilder()
            .WithConnectionProvider(new StubDbConnectionProvider(connection))
            .Build();

        await sut.ProcessQueue(CancellationToken.None);

        var stored = await connection.QueryAsync<OutboxEntry>($"select * from _outbox") ?? Enumerable.Empty<OutboxEntry>();

        Assert.Equal(
            expected: expectedMessagePayloads,
            actual: stored.Select(x => x.Payload)
        );
    }
}