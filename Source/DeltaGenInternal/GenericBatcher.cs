using DeltaGenCore;

namespace DeltaGenInternal;

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
        {{LoopRange(count, t => $"protected readonly GpuArray<T{t}> _bufferT{t};")}}

        public override ReadOnlySpan<GpuByteArray> Buffers => _buffers;
        private readonly GpuByteArray[] _buffers;

        public {{Name}}() : base()
        {
            if(Bindings.Length != {{count}})
                throw new ArgumentException($"Count of generic parameters in {nameof(RenderBatcher)} does not match count of Bindings", nameof(DescriptorSetsBindings));
            {{LoopRange(count, t => $"_bufferT{t} = new GpuArray<T{t}>(1);")}}
            _buffers = 
            [
                {{LoopRange(count, t => $"_bufferT{t},")}}
            ];
        }
    }
"""
)}}

""";
}
