using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Chomp.Save.Internal;

public class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return GD.StrToVar(reader.GetString()).AsVector3();
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options) {
        writer.WriteStringValue(GD.VarToStr(value));
    }
}
