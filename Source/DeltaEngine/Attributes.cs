using System.Runtime.CompilerServices;
namespace DeltaEngine;
public static class Attributes
{
    public const MethodImplOptions Inl = MethodImplOptions.AggressiveInlining | MethodImplOptions.NoOptimization;
    public const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
}