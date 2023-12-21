using System.Runtime.CompilerServices;
namespace Delta;
public static class Attributes
{
    public const MethodImplOptions Inl = MethodImplOptions.AggressiveInlining;
    public const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
}