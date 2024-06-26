﻿using System;
using System.Collections.Generic;

namespace Delta.Utilities;
internal static class Extensions
{
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
