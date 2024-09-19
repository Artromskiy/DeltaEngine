using Arch.Core;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DeltaEditor;

internal static class EditorFormatter
{
    private const string FloatFormat = "0.00";
    private static readonly Dictionary<float, string> _lookupFloat = [];
    private static readonly Dictionary<int, string> _lookupInt = [];
    private static readonly Dictionary<Guid, string> _lookupGuid = [];
    private static readonly Dictionary<EntityReference, string> _lookupEntityReference = [];
    private static readonly CultureInfo _editorCulture;
    static EditorFormatter()
    {
        _editorCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        _editorCulture.NumberFormat = (NumberFormatInfo)_editorCulture.NumberFormat.Clone();
        _editorCulture.NumberFormat.NumberDecimalSeparator = ".";
    }

    public static string ParseToString(this float value)=> value.ToString(FloatFormat, _editorCulture);
    public static string ParseToStringHighRes(this float value) => value.ToString("F", _editorCulture);

    public static bool ParseToFloat(this string? value, out float parsed)
    {
        parsed = default;
        if (string.IsNullOrEmpty(value))
            return true;
        else if (float.TryParse(value, NumberStyles.Float, _editorCulture, out parsed))
            return true;
        return false;
    }

    public static string ParseToString(this int value) => value.ToString();

    public static string LookupString(this EntityReference value)
    {
        if (!_lookupEntityReference.TryGetValue(value, out var result))
            _lookupEntityReference[value] = result = $"id: {value.Entity.Id}, ver: {value.Version}";
        return result;
    }
}
