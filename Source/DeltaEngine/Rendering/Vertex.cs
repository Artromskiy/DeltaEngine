using Silk.NET.Vulkan;
using System.Collections.Immutable;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DeltaEngine.Rendering;
[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex
{
    public readonly Vector2 pos;
    public readonly Vector3 color;

    public Vertex(Vector2 position, Vector3 color)
    {
        pos = position;
        this.color = color;
    }

    private static int? size;
    private static VertexInputBindingDescription? bindingDescription;
    private static ImmutableArray<VertexInputAttributeDescription>? attributeDesctiption;
    public static int Size => size ??= Marshal.SizeOf<Vertex>();
    public static VertexInputBindingDescription BindingDescription => bindingDescription ??= new()
    {
        Binding = 0,
        Stride = (uint)Size,
        InputRate = VertexInputRate.Vertex,
    };
    public static VertexInputBindingDescription GetBindingDescription(VertexAttribute vertexAttributeMask) => new()
    {
        Binding = 0,
        InputRate = VertexInputRate.Vertex,
        Stride = (uint)vertexAttributeMask.GetVertexSize()
    };


    public static unsafe void FillAttributeDesctiption(VertexInputAttributeDescription* ptr, VertexAttribute vertexAttributeMask)
    {
        int index = 0;
        int offset = 0;
        var attributes = VertexAttributeExtensions.VertexAttributes;
        for (int i = 0; i < attributes.Length; i++)
        {
            var item = attributes[i];
            if (vertexAttributeMask.HasFlag(item))
            {
                var size = item.GetAttributeSize();
                var format = size == 4 * 3 ? Format.R32G32B32Sfloat : Format.R32G32Sfloat;
                ptr[index++] = new()
                {
                    Binding = 0,
                    Format = format,
                    Location = (uint)i,
                    Offset = (uint)offset,
                };
                offset += size;
            }
        }
    }

    public static unsafe void FillAttributeDesctiption(VertexInputAttributeDescription* ptr)
    {
        for (int i = 0; i < AttributeDesctiption.Length; i++)
            ptr[i] = attributeDesctiption!.Value[i];
    }

    public static ImmutableArray<VertexInputAttributeDescription> AttributeDesctiption =>
        attributeDesctiption ??= ImmutableArray.Create(new VertexInputAttributeDescription[]
    {
        new()
        {
            Binding = 0,
            Location = 1,
            Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(pos)),
            Format = Format.R32G32Sfloat
        },
        new()
        {
            Binding = 0,
            Location = 2,
            Offset = (uint)Marshal.OffsetOf<Vertex>(nameof(color)),
            Format = Format.R32G32B32Sfloat
        }
    });
}
