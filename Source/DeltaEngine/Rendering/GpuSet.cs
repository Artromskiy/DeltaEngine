using Delta.Rendering.Internal;
using System.Collections.Generic;
using System.Numerics;

namespace Delta.Rendering;
internal class GpuSet<T> : GpuArray<T> where T : unmanaged
{
    private readonly HashSet<T> _values;

    public int Count => _values.Count;

    public GpuSet(RenderBase renderBase, uint length) : base(renderBase, length)
    {
        _values = [];
    }

    public bool Add(T value)
    {
        uint index = (uint)_values.Count;
        if (_values.Add(value))
        {
            if (index >= Length)
                Resize(BitOperations.RoundUpToPowerOf2(Length + 1));
            this[index] = value;
            return true;
        }
        return false;
    }

    public void Clear() => _values.Clear();
}
