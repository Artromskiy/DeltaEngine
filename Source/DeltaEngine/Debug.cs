using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace DeltaEngine;
internal static class Debug
{
    [Conditional("ASSERT")]
    [MethodImpl(Inl)]
    public static void Assert([DoesNotReturnIf(false)] bool condition) => System.Diagnostics.Debug.Assert(condition);
}