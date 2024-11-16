using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Chomp.Save.Internal;

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return GD.StrToVar(reader.GetString()).AsColor();
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options) {
        writer.WriteStringValue(GD.VarToStr(value));
    }
}
