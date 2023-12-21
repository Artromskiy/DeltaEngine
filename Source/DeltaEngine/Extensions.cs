using System;
using System.Collections.Generic;

namespace Delta;
internal static class Extensions
{

    /// <summary>
    /// Calls <see cref="IDisposable.Dispose"/> on each element of <paramref name="array"/>
    /// </summary>
    /// <param name="array"></param>
    public static void Dispose(this Array array)
    {
        foreach (var item in array)
            using (item as IDisposable) { };
    }

    /// <summary>
    /// Calls <see cref="IDisposable.Dispose"/> on each element of <paramref name="queue"/>
    /// </summary>
    /// <param name="array"></param>
    public static void Dispose<T>(this Queue<T> queue)
    {
        foreach (var item in queue)
            using (item as IDisposable) { };
    }
}
