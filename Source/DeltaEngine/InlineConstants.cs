global using Imp = System.Runtime.CompilerServices.MethodImplAttribute;
using System.Runtime.CompilerServices;

using DVG.Engine;
internal struct InlineConstants
{
    public const MethodImplOptions Inl = MethodImplOptions.AggressiveInlining;
    public const MethodImplOptions NoInl = MethodImplOptions.NoInlining;
    public const MethodImplOptions Sync = MethodImplOptions.Synchronized;
}
