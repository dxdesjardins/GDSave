using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Chomp.Save.Internal;

public class Vector2Converter : JsonConverter<Vector2>
{
    public override Vector2 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return GD.StrToVar(reader.GetString()).AsVector2();
    }

    public override void Write(Utf8JsonWriter writer, Vector2 value, JsonSerializerOptions options) {
        writer.WriteStringValue(GD.VarToStr(value));
    }
}
