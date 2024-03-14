using Delta.Rendering;
using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Delta.Files;

internal class Serialization
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
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { AddPrivateFieldsModifier }
            }
        };
    }

    public static void Serialize<T>(Stream utf8Stream, T value)
    {
        JsonSerializer.Serialize<T>(utf8Stream, value, _options);
    }

    public static void Serialize<T>(string path, T value)
    {
        using FileStream utf8Stream = File.Create(path);
        JsonSerializer.Serialize<T>(utf8Stream, value, _options);
    }

    public static T Deserialize<T>(string path)
    {
        using Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
        T result = JsonSerializer.Deserialize<T>(stream, _options);
        if (result is MaterialData md)
            Debug.Assert(md.shader != Guid.Empty);
        if (result is ShaderData sh)
            Debug.Assert(!sh.GetVertBytes().IsEmpty);
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

}
