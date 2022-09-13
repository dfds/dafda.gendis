using System.Collections;
using Confluent.Kafka;

namespace Dafda.Gendis.App.Configuration;

public static class Kafka
{
    public static void ConfigureKafkaProducer(this WebApplicationBuilder builder)
    {
        var prefix = builder.Configuration["GENDIS_PREFIX"] ?? "DEFAULT_KAFKA_";

        var configs = GetEnvironmentVariables()
            .Where(x => x.Key.StartsWith(prefix))
            .Select(x => ConfigurationKey.Parse(x.Key, x.Value, prefix))
            .ToDictionary(x => x.Key, x => x.Value);

        var shouldDisableOpinions = builder.Configuration["GENDIS_DISABLE_OPINIONS"] switch
        {
            "1" => true,
            "yes" => true,
            "YES" => true,
            _ => false
        };

        if (!shouldDisableOpinions)
        {
            configs["acks"] = "all";
            configs["enable.idempotence"] = "true";
            configs["max.in.flight.requests.per.connection"] = "1";
        }

        builder.Services.AddTransient<IProducer<string, string>>(_ =>
        {
            const string requiredConfiguration = "bootstrap.servers";
            if (!configs.ContainsKey(requiredConfiguration))
            {
                throw new Exception($"Error! Missing required producer configuration {requiredConfiguration}.");
            }

            return new ProducerBuilder<string, string>(configs).Build();
        });

        builder.Services.AddTransient<IProducer, KafkaProducer>();
    }

    private static IEnumerable<KeyValuePair<string, string>> GetEnvironmentVariables()
    {
        var result = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

        foreach (DictionaryEntry entry in result)
        {
            if (entry.Key is string k && entry.Value is string v)
            {
                yield return new KeyValuePair<string, string>(k, v);
            }
        }
    }
}