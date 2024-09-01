using Silk.NET.Vulkan;

namespace Delta.Rendering.Internal;
internal enum QueueType
{
    Graphics,
    Present,
    Compute,
    Transfer,
}

internal static class QueueTypeExtensions
{
    public static bool Supports(this QueueFlags flags, QueueType queueType)
    {
        return queueType switch
        {
            QueueType.Graphics => flags.HasFlag(QueueFlags.GraphicsBit),
            QueueType.Transfer => flags.HasFlag(QueueFlags.TransferBit),
            QueueType.Compute => flags.HasFlag(QueueFlags.ComputeBit),
            QueueType.Present => throw new System.NotImplementedException(),
            _ => throw new System.NotImplementedException(),
        };
    }
}