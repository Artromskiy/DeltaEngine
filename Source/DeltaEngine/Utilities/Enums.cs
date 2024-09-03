using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Delta.Utilities;

internal class Enums
{
    private readonly struct EnumBakedValues<T> where T : struct, Enum
    {
        public static readonly T[] values;
        public static readonly int count;
        public static readonly bool hasFlagsAttribute;
        public static readonly T allFlags;
        static EnumBakedValues()
        {
            values = Enum.GetValues<T>();
            count = SpanExtensions.Distinct<T>(values);
            values = values[..count];
            hasFlagsAttribute = typeof(T).IsDefined(typeof(FlagsAttribute), false);
        }
    }

    private readonly struct EnumBakedNames<T> where T : unmanaged, Enum
    {
        public static readonly Dictionary<T, string> valueToName = [];
        static EnumBakedNames()
        {
            var fields = typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
            int length = fields.Length;
            Span<bool> obsoletes = stackalloc bool[length];
            Span<T> values = stackalloc T[length];
            string[] names = ArrayPool<string>.Shared.Rent(length);
            for (int i = 0; i < length; i++)
            {
                var item = fields[i];
                obsoletes[i] = item.IsDefined(typeof(ObsoleteAttribute), false);
                values[i] = (T)item.GetValue(null)!;
                names[i] = item.Name;

                if (!obsoletes[i] && !valueToName.ContainsKey(values[i]))
                    valueToName[values[i]] = names[i];
            }

            for (int i = 0; i < length; i++)
                if (obsoletes[i] && !valueToName.ContainsKey(values[i]))
                    valueToName[values[i]] = names[i];

            ArrayPool<string>.Shared.Return(names);
        }
    }

    public static int GetCount<T>() where T : struct, Enum => EnumBakedValues<T>.count;
    public static ReadOnlySpan<T> GetValues<T>() where T : struct, Enum => EnumBakedValues<T>.values;

    /// <summary>
    /// Returns string representing current enum or enum flags.
    /// All elements are distinct by its undrelying type e.g. int, long, uint.
    /// If element is marked as obsolete, it's name will not be used
    /// except cases when replacement not found
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string ToString<T>(T value) where T : unmanaged, Enum
    {
        var valueToName = EnumBakedNames<T>.valueToName;
        const string Splitter = " | ";
        if (EnumBakedValues<T>.hasFlagsAttribute)
        {
            StringBuilder sb = new();

            foreach (var item in EnumBakedValues<T>.values)
                if (value.HasFlag(item))
                    sb.Append(valueToName[item]).Append(Splitter);

            sb.Length -= Splitter.Length;

            return sb.ToString();
        }
        else
            return valueToName[value];
    }
}
