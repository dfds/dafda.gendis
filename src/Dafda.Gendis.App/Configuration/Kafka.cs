using System.Collections;
using System.Text.RegularExpressions;
using Confluent.Kafka;

namespace Dafda.Gendis.App.Configuration;

public static class Kafka
{
    private static readonly string DefaultPrefixConvention = "DEFAULT_KAFKA_";

    private static readonly string[] RequiredConfigurationKeys =
    {
        ConfigurationKeys.BootstrapServers
    };

    private static readonly string[] DefaultConfigurationKeys =
    {
        ConfigurationKeys.BrokerVersionFallback,
        ConfigurationKeys.ApiVersionFallbackMs,
        ConfigurationKeys.SslCaLocation,
        ConfigurationKeys.SaslUsername,
        ConfigurationKeys.SaslPassword,
        ConfigurationKeys.SaslMechanisms,
        ConfigurationKeys.SecurityProtocol,
    };

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

        builder.Services.AddSingleton<IProducer<string, string>>(_ =>
        {
            return new ProducerBuilder<string, string>(configs).Build();
        });

        builder.Services.AddTransient<IProducer, KafkaProducer>();
    }
}

public static class ConfigurationKeys
{
    public static readonly string BootstrapServers = "bootstrap.servers";
    public static readonly string BrokerVersionFallback = "broker.version.fallback";
    public static readonly string ApiVersionFallbackMs = "api.version.fallback.ms";
    public static readonly string SslCaLocation = "ssl.ca.location";
    public static readonly string SaslUsername = "sasl.username";
    public static readonly string SaslPassword = "sasl.password";
    public static readonly string SaslMechanisms = "sasl.mechanisms";
    public static readonly string SecurityProtocol = "security.protocol";
}

public class ConfigurationKey
{
    private ConfigurationKey(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }
    public string Value { get; }

    public KeyValuePair<string, string> ToPair()
    {
        return new KeyValuePair<string, string>(Key, Value);
    }

    public static ConfigurationKey Parse(string key, string value, string? prefixConvention = null)
    {
        var properKey = key;

        if (!string.IsNullOrWhiteSpace(prefixConvention) && properKey.StartsWith(prefixConvention))
        {
            properKey = properKey.Substring(prefixConvention.Length);
        }

        properKey = properKey.ToLowerInvariant();
        properKey = Regex.Replace(properKey, "_+", ".");

        return new ConfigurationKey(properKey, value);
    }
}
