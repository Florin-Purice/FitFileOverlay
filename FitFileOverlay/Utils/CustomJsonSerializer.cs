using System.Text.Json;
using System.Text.Json.Serialization;

namespace FitFileOverlay.Utils;

public static class CustomJsonSerializer
{
    private static readonly JsonSerializerOptions serializerSettings;

    static CustomJsonSerializer()
    {
        serializerSettings = new JsonSerializerOptions()
        {
            WriteIndented = true,
            NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
        };
        serializerSettings.Converters.Add(new SKColorJsonConverter());
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, serializerSettings);
    }

    public static T? Deserialize<T>(this string json)
    {
        return JsonSerializer.Deserialize<T>(json, serializerSettings);
    }
}
