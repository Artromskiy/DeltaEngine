using Delta.Utilities;
using System;
using System.Numerics;
namespace Delta.Rendering;

/// <summary>
/// Vertex Attribute enum
/// enum order number must match layout location in Shader
/// </summary>
[Flags]
public enum VertexAttribute : int
{
    Pos3 = 1 << 0,
    Pos2 = 1 << 1,
    Col = 1 << 2,
    Tex = 1 << 3,
    Norm = 1 << 4,
    Tan = 1 << 5,
    Binorm = 1 << 6,
    Bitan = 1 << 7,
}

internal static class VertexAttributeExtensions
{
    private static readonly int _attributesCount = Enums.GetValues<VertexAttribute>().Length;
    private static VertexAttribute GetAttribute(int location) => (VertexAttribute)(1 << location);
    public static int GetAttributeLocation(this VertexAttribute attribute) => BitOperations.Log2((uint)attribute);
    public static int GetAttributeSize(this VertexAttribute attribute) => GetAttributeSize(GetAttributeLocation(attribute));
    private static int GetAttributeSize(int location)
    {
        const int float4 = 4 * 4;
        const int float3 = 4 * 3;
        const int float2 = 4 * 2;
        return location switch
        {
            0 => float3,  // position3
            1 => float2,  // position2
            2 => float4,  // color
            3 => float2,  // uv
            4 => float3,  // normal
            5 => float3,  // binormal
            6 => float3,  // tangent
            7 => float3,  // bitangent
            _ => float4,  // default for user data
        };
    }
    public static EnumerableVertexAttributeMask Iterate(this VertexAttribute vertexAttributeMask) => new(vertexAttributeMask);

    public struct VertexAttributeMaskElement(VertexAttribute value, int location, int size)
    {
        public VertexAttribute value = value;
        public int location = location;
        public int size = size;
    }


    public ref struct EnumerableVertexAttributeMask
    {
        private readonly VertexAttribute _mask;
        private int _position = -1;

        internal EnumerableVertexAttributeMask(VertexAttribute mask) => _mask = mask;

        public bool MoveNext()
        {
            _position++;
            while (_position < _attributesCount && !_mask.HasFlag(GetAttribute(_position)))
                _position++;
            return _position < _attributesCount;
        }
        public readonly VertexAttributeMaskElement Current => new(GetAttribute(_position), _position, GetAttributeSize(_position));
        public readonly EnumerableVertexAttributeMask GetEnumerator() => this;
    }

    public static int GetVertexSize(this VertexAttribute vertexAttributeMask)
    {
        int size = 0;
        for (int i = 0; i < _attributesCount; i++)
            size += vertexAttributeMask.HasFlag(GetAttribute(i)) ? GetAttributeSize(i) : 0;
        return size;
    }

    public static int GetAttributesCount(this VertexAttribute vertexAttributeMask)
    {
        int size = 0;
        for (int i = 0; i < _attributesCount; i++)
            if (vertexAttributeMask.HasFlag(GetAttribute(i)))
                size++;
        return size;
    }

    public static (int location, int size) GetLocationAndSize(this VertexAttribute attribute)
    {
        int location = BitOperations.Log2((uint)attribute);
        return (location, GetAttributeSize(location));
    }

}