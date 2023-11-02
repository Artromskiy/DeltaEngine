using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Numerics;

namespace DeltaEngine.Rendering;

/// <summary>
/// Vertex Attribute enum
/// enum order number must match layout location in shader
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

public static class VertexAttributeExtensions
{
    private static readonly int[] VertexAttributeSizes = new int[]
    {
        4 * 3,  // position3
        4 * 2,  // position2
        4 * 3,  // color
        4 * 2,  // uv
        4 * 3,  // normal
        4 * 3,  // binormal
        4 * 3,  // tangent
        4 * 3,  // bitangent
    };

    private static ImmutableArray<VertexAttribute>? _vertexAttributes;
    public static ImmutableArray<VertexAttribute> VertexAttributes => _vertexAttributes ??= ImmutableArray.Create(Enum.GetValues<VertexAttribute>());

    public static IterateVertexAttributeMask Iterate(this VertexAttribute vertexAttributeMask) => new(vertexAttributeMask);

    public struct IterateVertexAttributeMask : IEnumerator<VertexAttribute>, IEnumerable<VertexAttribute>
    {
        private readonly VertexAttribute _vertexAttributeMask;
        private int _position;
        public readonly void Dispose() { }
        public void Reset() => _position = -1;
        public bool MoveNext()
        {
            while (_position < VertexAttributes.Length || _vertexAttributeMask.HasFlag(Current))
                _position++;
            return _position < VertexAttributes.Length;
        }
        public readonly VertexAttribute Current => VertexAttributes[_position];
        public readonly IEnumerator<VertexAttribute> GetEnumerator() => this;
        readonly object IEnumerator.Current => Current;
        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IterateVertexAttributeMask(VertexAttribute vertexAttributeMask)
        {
            _position = -1;
            _vertexAttributeMask = vertexAttributeMask;
        }
    }

    public static int GetVertexSize(this VertexAttribute vertexAttributeMask)
    {
        int size = 0;
        var attributes = VertexAttributes;
        for (int i = 0; i < attributes.Length; i++)
            size += vertexAttributeMask.HasFlag(attributes[i]) ? VertexAttributeSizes[i] : 0;
        return size;
    }
    public static int GetAttributesCount(this VertexAttribute vertexAttributeMask)
    {
        int size = 0;
        var attributes = VertexAttributes;
        for (int i = 0; i < attributes.Length; i++)
            size += vertexAttributeMask.HasFlag(attributes[i]) ? 1 : 0;
        return size;
    }

    public static (int location, int size) GetLocationAndSize(this VertexAttribute attribute)
    {
        int location = BitOperations.Log2((uint)attribute);
        return (location, VertexAttributeSizes[location]);
    }
    public static int GetAttributeLocation(this VertexAttribute attribute) => GetLocationAndSize(attribute).location;
    public static int GetAttributeSize(this VertexAttribute attribute) => GetLocationAndSize(attribute).size;
}