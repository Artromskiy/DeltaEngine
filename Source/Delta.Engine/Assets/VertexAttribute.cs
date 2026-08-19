using System;
using System.Numerics;

namespace Delta.Engine.Assets;

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
    private const int AttributeCount = 8;

    public static int GetAttributeLocation(this VertexAttribute attribute) => BitOperations.Log2((uint)attribute);

    public static int GetAttributeSize(this VertexAttribute attribute) => GetAttributeSize(attribute.GetAttributeLocation());

    private static int GetAttributeSize(int location) => location switch
    {
        0 => sizeof(float) * 3,
        1 => sizeof(float) * 2,
        2 => sizeof(float) * 4,
        3 => sizeof(float) * 2,
        4 or 5 or 6 or 7 => sizeof(float) * 3,
        _ => sizeof(float) * 4,
    };

    public static EnumerableVertexAttributeMask Iterate(this VertexAttribute mask) => new(mask);

    public static int GetVertexSize(this VertexAttribute mask)
    {
        var size = 0;
        for (var location = 0; location < AttributeCount; location++)
        {
            if ((mask & (VertexAttribute)(1 << location)) != 0)
                size += GetAttributeSize(location);
        }

        return size;
    }

    public ref struct EnumerableVertexAttributeMask
    {
        private readonly VertexAttribute _mask;
        private int _position = -1;

        internal EnumerableVertexAttributeMask(VertexAttribute mask) => _mask = mask;

        public bool MoveNext()
        {
            _position++;
            while (_position < AttributeCount && (_mask & (VertexAttribute)(1 << _position)) == 0)
                _position++;
            return _position < AttributeCount;
        }

        public readonly VertexAttributeMaskElement Current => new(
            (VertexAttribute)(1 << _position),
            _position,
            GetAttributeSize(_position));

        public readonly EnumerableVertexAttributeMask GetEnumerator() => this;
    }

    public readonly struct VertexAttributeMaskElement(VertexAttribute value, int location, int size)
    {
        public readonly VertexAttribute value = value;
        public readonly int location = location;
        public readonly int size = size;
    }
}
