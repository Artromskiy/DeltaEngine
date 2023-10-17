using System;

namespace DeltaEngine
{
    internal static class SpanExtensions
    {
        public static bool Exist<T>(this ReadOnlySpan<T> span, Predicate<T> match) where T : struct
        {
            foreach (var item in span)
                if (match(item))
                    return true;
            return false;
        }
        public static T Find<T>(this ReadOnlySpan<T> span, Predicate<T> match) where T: struct
        {
            foreach (var item in span)
                if (match(item))
                    return item;
            return default;
        }

        public static bool Exist<T>(this Span<T> span, Predicate<T> match) where T : struct
        {
            ReadOnlySpan<T> ro = span;
            return Exist(ro, match);
        }
        public static T Find<T>(this Span<T> span, Predicate<T> match) where T : struct
        {
            ReadOnlySpan<T> ro = span;
            return Find(ro, match);
        }
    }
}
