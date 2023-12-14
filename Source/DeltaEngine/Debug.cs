using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DeltaEngine;
internal static class Debug
{
    [Conditional("ASSERT")]
    public static void Assert([DoesNotReturnIf(false)] bool condition) => System.Diagnostics.Debug.Assert(condition);
}