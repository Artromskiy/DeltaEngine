using Silk.NET.Vulkan;
using System;

namespace Delta.Rendering.Internal;
internal interface IShaderLayouts
{
    public ReadOnlySpan<DescriptorSetLayout> Layouts { get; }
}
