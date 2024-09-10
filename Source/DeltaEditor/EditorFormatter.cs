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
    private const NumberStyles FloatNumberStyles = NumberStyles.Float;
    private static readonly CultureInfo _editorCulture;
    static EditorFormatter()
    {
        _editorCulture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
        _editorCulture.NumberFormat = (NumberFormatInfo)_editorCulture.NumberFormat.Clone();
        _editorCulture.NumberFormat.NumberDecimalSeparator = ".";
    }

    public static string ParseToString(this float value)
    {
        var val = value.ToString(FloatFormat, _editorCulture);
        return val;
    }

    public static bool ParseToFloat(this string? value, out float parsed)
    {
        parsed = default;
        if (string.IsNullOrEmpty(value))
            return true;
        else if (float.TryParse(value, NumberStyles.Float, _editorCulture, out parsed))
            return true;
        return false;
    }

    public static string LookupString(this int value)
    {
        if (!_lookupInt.TryGetValue(value, out var result))
            _lookupInt[value] = result = value.ToString();
        return result;
    }

    public static string LookupString(this Guid value)
    {
        if (!_lookupGuid.TryGetValue(value, out var result))
        {
            Span<byte> guidBytes = stackalloc byte[16];
            value.TryWriteBytes(guidBytes);
            _lookupGuid[value] = result = Convert.ToBase64String(guidBytes);
        }
        return result;
    }

    public static string LookupString(this EntityReference value)
    {
        if (!_lookupEntityReference.TryGetValue(value, out var result))
            _lookupEntityReference[value] = result = $"id: {value.Entity.Id}, ver: {value.Version}";
        return result;
    }
}
