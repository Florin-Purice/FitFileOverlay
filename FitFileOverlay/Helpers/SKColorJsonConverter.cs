using SkiaSharp;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace FitFileOverlay.Helpers;

public class SKColorJsonConverter : JsonConverter<SKColor>
{
    public override SKColor Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string value = reader.GetString() ?? "#000000";
        SKColor color = SKColor.Parse(value);
        return color;
    }

    public override void Write(Utf8JsonWriter writer, SKColor value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
