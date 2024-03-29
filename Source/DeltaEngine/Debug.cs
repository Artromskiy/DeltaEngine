﻿using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Delta;
internal static class Debug
{
    [Conditional("ASSERT")]
    public static void Assert([DoesNotReturnIf(false)] bool condition) => System.Diagnostics.Debug.Assert(condition);
    [Conditional("ASSERT")]
    public static void Assert([DoesNotReturnIf(false)] bool condition, string message) => System.Diagnostics.Debug.Assert(condition, message);
}