using System;
using Dafda.Gendis.App;
using Dafda.Gendis.Tests.TestDoubles;
using Microsoft.Extensions.Logging.Abstractions;

namespace Dafda.Gendis.Tests.Builders;

public class QueueProcessorBuilder
{
    private IDbConnectionProvider _connectionProvider;
    private IProducer _producer;
    private SystemTime _systemTime;

    public QueueProcessorBuilder()
    {
        _connectionProvider = Dummy.Of<IDbConnectionProvider>();
        _producer = Dummy.Of<IProducer>();
        _systemTime = new SystemTime(() => new DateTime(2000, 1, 1));
    }

    public QueueProcessorBuilder WithConnectionProvider(IDbConnectionProvider connectionProvider)
    {
        _connectionProvider = connectionProvider;
        return this;
    }

    public QueueProcessorBuilder WithProducer(IProducer producer)
    {
        _producer = producer;
        return this;
    }

    public QueueProcessorBuilder WithSystemTime(SystemTime systemTime)
    {
        _systemTime = systemTime;
        return this;
    }

    public QueueProcessorBuilder WithSystemTime(DateTime systemTime)
    {
        _systemTime = new SystemTime(() => systemTime);
        return this;
    }

    public QueueProcessor Build()
    {
        return new QueueProcessor(
            logger: NullLogger<QueueProcessor>.Instance,
            connectionProvider: _connectionProvider,
            producer: _producer,
            systemTime: _systemTime
        );
    }
}