using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DeltaEngine.Rendering;
internal struct Vertex
{
    public Vector2 pos;
    public Vector3 color;

    public static unsafe VertexInputBindingDescription BindingDescription()
    {
        VertexInputBindingDescription bindingDesctiption = new()
        {
            Binding = 0,
            InputRate = VertexInputRate.Vertex,
            Stride = (uint)sizeof(Vertex)
        };
        return bindingDesctiption;
    }
}
