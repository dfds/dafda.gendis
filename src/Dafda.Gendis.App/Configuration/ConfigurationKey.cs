using System.Text.RegularExpressions;

namespace Dafda.Gendis.App.Configuration;

public class ConfigurationKey
{
    private ConfigurationKey(string key, string value)
    {
        Key = key;
        Value = value;
    }

    public string Key { get; }
    public string Value { get; }

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