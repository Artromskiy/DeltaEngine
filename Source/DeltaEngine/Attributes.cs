using System.Runtime.CompilerServices;
namespace Delta;
internal static class Attributes
{
    public const MethodImplOptions Inl = MethodImplOptions.AggressiveInlining;
    public const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
}