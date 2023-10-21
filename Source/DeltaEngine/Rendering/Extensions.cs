using Silk.NET.Vulkan;

namespace DeltaEngine.Rendering;
internal static class Extensions
{

    public static ColorComponentFlags ColorComponentFlagsAll =>
        ColorComponentFlags.RBit |
        ColorComponentFlags.GBit |
        ColorComponentFlags.BBit |
        ColorComponentFlags.ABit;

}
