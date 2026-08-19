using System.Runtime.InteropServices;
using Delta.Engine.Integration;
using Delta.Render.Core;
using Delta.Render.Vulkan;

namespace Delta.Engine.ComputeSmoke;

internal static class Program
{
    private static async Task<int> Main()
    {
        using var journal = new EngineWorldChangeJournal();
        using IEngineWorldChangeSubscription renderSubscription = journal.Subscribe("compute-renderer");

        var world = new ComputeWorld(journal, 64);
        world.Update();

        byte[] shader = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "fixtures", "compute_double.spv"));
        var metadata = new ComputeShaderMetadata(
            ComputeAbiLayout.Std430,
            64,
            1,
            1,
            new[]
            {
                new ComputeDescriptorBinding(
                    0,
                    0,
                    ComputeDescriptorKind.StorageBuffer,
                    ComputeBufferAccess.ReadWrite)
            });

        await using var renderer = new VulkanRenderer(new VulkanRendererOptions());
        await using IComputeDevice device = renderer.CreateComputeDevice();
        await using IComputePipeline pipeline = device.CreateComputePipeline(shader, in metadata);
        await using IComputeStorageBuffer buffer = device.CreateStorageBuffer((ulong)world.Values.Length * sizeof(uint));

        var computeRenderer = new ComputeRenderer(device, pipeline, buffer, world.Values);
        new EngineWorldChangeRenderAdapter().Render(default, renderSubscription, computeRenderer);

        return computeRenderer.Succeeded ? 0 : 1;
    }

    private sealed class ComputeWorld
    {
        private readonly IEngineWorldChangeRecorder _changes;

        public ComputeWorld(IEngineWorldChangeRecorder changes, int valueCount)
        {
            _changes = changes;
            Values = new uint[valueCount];
        }

        public uint[] Values { get; }

        public void Update()
        {
            for (var i = 0; i < Values.Length; i++)
            {
                Values[i] = (uint)i;
                _changes.Record(new EngineWorldChange(
                    (ulong)i,
                    "ComputeValue",
                    EngineWorldChangeKind.ComponentChanged));
            }
        }
    }

    private sealed class ComputeRenderer : IEngineRenderChangeConsumer
    {
        private readonly IComputeDevice _device;
        private readonly IComputePipeline _pipeline;
        private readonly IComputeStorageBuffer _buffer;
        private readonly uint[] _input;

        public ComputeRenderer(
            IComputeDevice device,
            IComputePipeline pipeline,
            IComputeStorageBuffer buffer,
            uint[] input)
        {
            _device = device;
            _pipeline = pipeline;
            _buffer = buffer;
            _input = input;
        }

        public bool Succeeded { get; private set; }

        public void Render(EngineFrameContext frame, ReadOnlySpan<EngineWorldChange> changes)
        {
            if (changes.Length != _input.Length)
            {
                Console.Error.WriteLine($"Expected {_input.Length} world changes, received {changes.Length}.");
                return;
            }

            ReadOnlySpan<byte> inputBytes = MemoryMarshal.AsBytes(_input.AsSpan());
            if (!_device.Upload(_buffer, inputBytes))
            {
                Console.Error.WriteLine("SSBO upload failed.");
                return;
            }

            ComputeDispatchResult dispatch = _device.Dispatch(
                _pipeline,
                new[] { new ComputeBufferBinding(0, 0, _buffer) },
                1);
            if (!dispatch.Succeeded || dispatch.Status != ComputeDispatchStatus.Executed)
            {
                Console.Error.WriteLine(dispatch.Error ?? "Compute dispatch failed.");
                return;
            }

            var output = new byte[inputBytes.Length];
            if (!_device.Readback(_buffer, output))
            {
                Console.Error.WriteLine("SSBO readback failed.");
                return;
            }

            ReadOnlySpan<uint> actual = MemoryMarshal.Cast<byte, uint>(output);
            for (var i = 0; i < actual.Length; i++)
            {
                if (actual[i] != _input[i] * 2 + 1)
                {
                    Console.Error.WriteLine($"Compute result mismatch at {i}: {actual[i]}.");
                    return;
                }
            }

            Console.WriteLine($"compute-changes={changes.Length} dispatch=1 readback={actual.Length} pass=oracle");
            Succeeded = true;
        }
    }
}
