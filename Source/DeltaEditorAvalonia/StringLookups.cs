using Arch.Core;
using System;
using System.Collections.Generic;
namespace DeltaEditor.Tools
{
    internal static class StringLookups
    {
        private const string FloatFormat = "0.00";
        private static readonly Dictionary<float, string> _lookupFloat = [];
        private static readonly Dictionary<int, string> _lookupInt = [];
        private static readonly Dictionary<Guid, string> _lookupGuid = [];
        private static readonly Dictionary<EntityReference, string> _lookupEntityReference = [];

        public static string LookupString(this float value)
        {
            if (!_lookupFloat.TryGetValue(value, out var result))
                _lookupFloat[value] = result = value.ToString(FloatFormat);
            return result;
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
}
