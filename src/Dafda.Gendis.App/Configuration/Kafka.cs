using System.Collections;
using System.Text.Json;
using Confluent.Kafka;

namespace Dafda.Gendis.App.Configuration;

public static class Kafka
{
    private static readonly string sslCertificateLocationKey = "ssl.ca.location";

    public static void ConfigureKafkaProducer(this WebApplicationBuilder builder)
    {
        var prefix = builder.Configuration["GENDIS_PREFIX_FOR_KAFKA"] ?? "DEFAULT_KAFKA_";

        var configs = GetEnvironmentVariables()
            .Where(x => x.Key.StartsWith(prefix))
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => ConfigurationKey.Parse(x.Key, x.Value, prefix))
            .ToDictionary(x => x.Key, x => x.Value);

        if (!configs.ContainsKey(sslCertificateLocationKey))
        {
            var certificateLocation = Environment.GetEnvironmentVariable("GENDIS_KAFKA_SSL_CA_LOCATION");
            if (!string.IsNullOrWhiteSpace(certificateLocation))
            {
                configs.Add(sslCertificateLocationKey, certificateLocation);
            }
        }

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

        Console.WriteLine("config: " + JsonSerializer.Serialize(configs, new JsonSerializerOptions{WriteIndented = true}));

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