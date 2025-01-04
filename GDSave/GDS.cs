using Godot;
using System;
using System.Collections.Generic;
using Chomp.Essentials;
using System.Text.Json;
using Chomp.Save.Internal;
using System.Text.Encodings.Web;
using System.Text.Json.Serialization.Metadata;
using System.Reflection;

namespace Chomp.Save;

public static class GDS
{
    private static readonly JsonSerializerOptions options = GetSerializerOptions(SaveSettings.Instance.StorageConfig);

    public static JsonSerializerOptions GetSerializerOptions(StorageConfig storageConfig) {
        var options = new JsonSerializerOptions {
            WriteIndented = storageConfig.WriteIndented,
            IncludeFields = storageConfig.IncludeFields,
            TypeInfoResolver = storageConfig.IncludeFields && storageConfig.IncludePrivateFields ?
                new DefaultJsonTypeInfoResolver { Modifiers = {  AddAllPrivateFields } } : JsonSerializerOptions.Default.TypeInfoResolver,
            Encoder = storageConfig.UnsafeRelaxedJsonEscaping ? JavaScriptEncoder.UnsafeRelaxedJsonEscaping : JavaScriptEncoder.Default,
        };
        if (!storageConfig.IncludeFields) {
            options.Converters.Add(new Vector2Converter());
            options.Converters.Add(new Vector3Converter());
            options.Converters.Add(new ColorConverter());
            options.Converters.Add(new Transform3DConverter());
        }
        return options;
    }

    private static void AddAllPrivateFields(JsonTypeInfo jsonTypeInfo) {
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            return;
        foreach (FieldInfo field in jsonTypeInfo.Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)) {
            JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
            jsonPropertyInfo.Get = field.GetValue;
            jsonPropertyInfo.Set = field.SetValue;
            jsonTypeInfo.Properties.Add(jsonPropertyInfo);
        }
    }

    public static string Serialize(object value) {
        return JsonSerializer.Serialize(value, options);
    }

    public static T Deserialize<T>(string value) {
        return JsonSerializer.Deserialize<T>(value, options);
    }

    public static string SerializeMetaData(object value) {
        return JsonSerializer.Serialize(value);
    }

    public static T DeserializeMetaData<T>(string value) {
        return JsonSerializer.Deserialize<T>(value);
    }

    public static void Log(string text) {
        if (SaveSettings.Instance.EnableLogging)
            GDE.Log(text, 2);
    }

    public static void LogErr(string text) {
        if (SaveSettings.Instance.EnableLogging)
            GDE.LogErr(text, 2);
    }
}
