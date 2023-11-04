using Silk.NET.Vulkan;
using System.Numerics;
using System.Runtime.InteropServices;

namespace DeltaEngine.Rendering;
[StructLayout(LayoutKind.Sequential)]
public readonly struct Vertex
{
    public readonly Vector2 pos;
    public readonly Vector4 color;

    public Vertex(Vector2 position, Vector3 color)
    {
        pos = position;
        this.color = new(color, 1);
    }

    public static int Size => 6 * 4;
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
        foreach (var attrib in vertexAttributeMask.Iterate())
        {
            var format = attrib.size == 4 * 3 ? Format.R32G32B32Sfloat : attrib.size == 4*4? Format.R32G32B32A32Sfloat: Format.R32G32Sfloat;
            ptr[index++] = new()
            {
                Binding = 0,
                Format = format,
                Location = (uint)attrib.location,
                Offset = (uint)offset,
            };
            offset += attrib.size;
        }
    }
}
