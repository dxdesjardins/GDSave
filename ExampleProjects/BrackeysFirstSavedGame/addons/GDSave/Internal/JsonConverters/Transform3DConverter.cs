using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

namespace Chomp.Save.Internal;

public class Transform3DConverter : JsonConverter<Transform3D>
{
    public override Transform3D Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) {
        return GD.StrToVar(reader.GetString()).AsTransform3D();
    }

    public override void Write(Utf8JsonWriter writer, Transform3D value, JsonSerializerOptions options) {
        writer.WriteStringValue(GD.VarToStr(value));
    }
}
