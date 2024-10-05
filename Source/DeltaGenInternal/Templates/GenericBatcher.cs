using DeltaGenInternal.Core;

namespace DeltaGenInternal.Templates;

internal class GenericBatcher : Template
{
    public override string Name => nameof(GenericBatcher);
    public override string ToString() =>
$$"""

using Delta.Rendering.Collections;
using System;

namespace Delta.Rendering;

{{LoopRange(1, Constants.VariadicCount, count =>
$$""" 
    internal abstract class {{Name}}<{{JoinRange(count, t => $"T{t}", ",")}}> : RenderBatcher
    {{LoopRange(count, t => $"where T{t} : unmanaged")}}    
    {
        public override ReadOnlySpan<GpuByteArray> Buffers => _buffers;
        private readonly GpuByteArray[] _buffers =
        [
            {{LoopRange(count, t => $"new GpuArray<T{t}>(1),")}}
        ];
    }
"""
)}}

""";
}
