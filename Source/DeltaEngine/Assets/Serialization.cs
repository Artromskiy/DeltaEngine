using Arch.Core;
using Arch.Persistence;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Delta.Assets;

internal static class Serialization
{
    private static readonly JsonSerializerOptions _options;
    public static JsonSerializerOptions Options => _options;

    static Serialization()
    {
        _options = new(JsonSerializerOptions.Default)
        {
            IgnoreReadOnlyFields = false,
            IncludeFields = true,
            IgnoreReadOnlyProperties = true,
            AllowTrailingCommas = false,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { AddPrivateFieldsModifier }
            },
        };
        _options.Converters.Add(new WorldConverter());
    }

    public static void Serialize<T>(Stream stream, T value)
    {
        JsonSerializer.Serialize(stream, value, _options);
    }

    public static void Serialize<T>(string path, T value)
    {
        using FileStream stream = File.Create(path);
        JsonSerializer.Serialize(stream, value, _options);
    }

    public static T Deserialize<T>(string path)
    {
        using Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        T result = JsonSerializer.Deserialize<T>(stream, _options);
        return result;
    }

    private static void AddPrivateFieldsModifier(JsonTypeInfo jsonTypeInfo)
    {
        if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
            return;

        foreach (FieldInfo field in jsonTypeInfo.Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
        {
            JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
            jsonPropertyInfo.Get = field.GetValue;
            jsonPropertyInfo.Set = field.SetValue;

            jsonTypeInfo.Properties.Add(jsonPropertyInfo);
        }
    }

    private class WorldConverter : JsonConverter<World>
    {
        private readonly ArchJsonSerializer _serializer = new();
        public override World? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return _serializer.Deserialize(reader.GetBytesFromBase64());
        }
        public override void Write(Utf8JsonWriter writer, World value, JsonSerializerOptions options)
        {
            writer.WriteBase64StringValue(_serializer.Serialize(value));
        }
    }
}
