using System.Collections.Generic;

namespace Delta.Utilities;
public class NullSafeComparer<T> : IComparer<T>
{
    private static readonly NullSafeComparer<T> _defaultComparer = new();
    public static NullSafeComparer<T> Default => _defaultComparer;

    public int Compare(T? x, T? y)
    {
        if (x == null || y == null)
            return NullCompare(x, y);
        return Comparer<T>.Default.Compare(x, y);
    }

    private static int NullCompare(T? x, T? y)
    {
        return (x, y) switch
        {
            (null, null) => 0,
            (null, _) => 1,
            (_, null) => -1,
            _ => 0
        };
    }

}
