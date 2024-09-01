using System;

namespace Delta.Utilities;

internal class Enums
{
    private readonly struct EnumBakedValues<T> where T : struct, Enum
    {
        public static readonly T[] Values = Enum.GetValues<T>();
        public static readonly int Count = Enum.GetValues<T>().Length;
        static EnumBakedValues()
        {
            Values = Enum.GetValues<T>();
            Count = Values.Length;
        }
    }

    public static int GetCount<T>() where T : struct, Enum => EnumBakedValues<T>.Count;
    public static ReadOnlySpan<T> GetValues<T>() where T : struct, Enum => EnumBakedValues<T>.Values;
}
