using System;
using System.Collections;
using System.Collections.Generic;
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

    private static int GetAttributeSize(int location)
    {
        return location switch
        {
            0 => 4 * 3,  // position3
            1 => 4 * 2,  // position2
            2 => 4 * 4,  // color
            3 => 4 * 2,  // uv
            4 => 4 * 3,  // normal
            5 => 4 * 3,  // binormal
            6 => 4 * 3,  // tangent
            7 => 4 * 3,  // bitangent
            _ => 4 * 4,  // default for user data
        };
    }

    private static VertexAttribute GetAttribute(int location) => (VertexAttribute)(1 << location);
    private static int AttributesCount => Enum.GetValues<VertexAttribute>().Length;
    public struct VertexAttributeMaskElement(VertexAttribute value, int location, int size)
    {
        public VertexAttribute value = value;
        public int location = location;
        public int size = size;
    }

    public static IterateVertexAttributeMask Iterate(this VertexAttribute vertexAttributeMask) => new(vertexAttributeMask);

    public struct IterateVertexAttributeMask(VertexAttribute vertexAttributeMask) : IEnumerator<VertexAttributeMaskElement>, IEnumerable<VertexAttributeMaskElement>
    {
        private readonly VertexAttribute _vertexAttributeMask = vertexAttributeMask;
        private int _position = -1;
        public readonly void Dispose() { }
        public void Reset() => _position = -1;
        public bool MoveNext()
        {
            _position++;
            while (_position < AttributesCount && !_vertexAttributeMask.HasFlag(GetAttribute(_position)))
                _position++;
            return _position < AttributesCount;
        }
        public readonly VertexAttributeMaskElement Current => new(GetAttribute(_position), _position, GetAttributeSize(_position));
        public readonly IEnumerator<VertexAttributeMaskElement> GetEnumerator() => this;
        readonly object IEnumerator.Current => Current;
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static int GetVertexSize(this VertexAttribute vertexAttributeMask)
    {
        int size = 0;
        for (int i = 0; i < AttributesCount; i++)
            size += vertexAttributeMask.HasFlag(GetAttribute(i)) ? GetAttributeSize(i) : 0;
        return size;
    }

    public static int GetAttributesCount(this VertexAttribute vertexAttributeMask)
    {
        int size = 0;
        for (int i = 0; i < AttributesCount; i++)
            if (vertexAttributeMask.HasFlag(GetAttribute(i)))
                size++;
        return size;
    }

    public static (int location, int size) GetLocationAndSize(this VertexAttribute attribute)
    {
        int location = BitOperations.Log2((uint)attribute);
        return (location, GetAttributeSize(location));
    }

    public static int Location(this VertexAttribute attribute) => BitOperations.Log2((uint)attribute);
    public static int Size(this VertexAttribute attribute) => GetAttributeSize(Location(attribute));
}