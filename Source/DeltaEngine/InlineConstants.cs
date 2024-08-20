using System.Runtime.CompilerServices;
namespace Delta;
internal struct InlineConstants
{
    public const MethodImplOptions Inl = MethodImplOptions.AggressiveInlining;
    public const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
}