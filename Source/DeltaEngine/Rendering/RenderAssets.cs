using Delta.Files;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using Buffer = Silk.NET.Vulkan.Buffer;


namespace Delta.Rendering;
internal class RenderAssets : IDisposable
{
    private readonly Dictionary<GuidAsset<ShaderData>, (Pipeline pipeline, VertexAttribute mask)> _renderToPipe;
    private readonly Dictionary<GuidAsset<MeshData>, MeshHandler> _renderToMeshHandler;

    private readonly RenderBase _renderBase;

    public RenderAssets(RenderBase renderBase)
    {
        _renderBase = renderBase;
        _renderToPipe = [];
        _renderToMeshHandler = [];
    }

    public (Pipeline pipeline, VertexAttribute mask) GetPipelineAndAttributes(GuidAsset<ShaderData> shader)
    {
        if (!_renderToPipe.TryGetValue(shader, out var pipe))
            _renderToPipe[shader] = pipe = (RenderHelper.CreateGraphicsPipeline(shader.GetAsset(), _renderBase, out var mask), mask);
        return pipe;
    }

    public (Buffer vertices, Buffer indices) GetVertexIndexBuffers(GuidAsset<MeshData> mesh, VertexAttribute vertexMask)
    {
        if (!_renderToMeshHandler.TryGetValue(mesh, out var meshHandler))
        {
            var meshAsset = mesh.GetAsset();
            var (vertexBuffer, vertexMemory) = RenderHelper.CreateVertexBuffer(_renderBase, meshAsset, vertexMask);
            var (indexBuffer, indexMemory) = RenderHelper.CreateIndexBuffer(_renderBase, meshAsset);
            _renderToMeshHandler[mesh] = meshHandler = new(vertexBuffer, vertexMemory, indexBuffer, indexMemory, meshAsset.GetIndicesCount());
        }
        return (meshHandler.vertices, meshHandler.indices);
    }

    public (Buffer vertices, Buffer indices, uint indicesCount) GetVertexIndexBuffersAndCount(GuidAsset<MeshData> mesh, VertexAttribute vertexMask)
    {
        if (!_renderToMeshHandler.TryGetValue(mesh, out var meshHandler))
        {
            var meshAsset = mesh.GetAsset();
            var (vertexBuffer, vertexMemory) = RenderHelper.CreateVertexBuffer(_renderBase, meshAsset, vertexMask);
            var (indexBuffer, indexMemory) = RenderHelper.CreateIndexBuffer(_renderBase, meshAsset);
            _renderToMeshHandler[mesh] = meshHandler = new(vertexBuffer, vertexMemory, indexBuffer, indexMemory, meshAsset.GetIndicesCount());
        }
        return (meshHandler.vertices, meshHandler.indices, meshHandler.indicesCount);
    }

    public void Dispose()
    {
        unsafe
        {
            foreach (var item in _renderToPipe)
                _renderBase.vk.DestroyPipeline(_renderBase.deviceQ.device, item.Value.pipeline, null);
            foreach (var item in _renderToMeshHandler)
            {
                _renderBase.vk.DestroyBuffer(_renderBase.deviceQ.device, item.Value.vertices, null);
                _renderBase.vk.DestroyBuffer(_renderBase.deviceQ.device, item.Value.indices, null);
                _renderBase.vk.FreeMemory(_renderBase.deviceQ.device, item.Value.verticesMemory, null);
                _renderBase.vk.FreeMemory(_renderBase.deviceQ.device, item.Value.indicesMemory, null);
            }
        }
        _renderToPipe.Clear();
        _renderToMeshHandler.Clear();
    }

    private readonly struct MeshHandler(Buffer vertices, DeviceMemory verticesMemory, Buffer indices, DeviceMemory indicesMemory, uint indicesCount)
    {
        public readonly Buffer vertices = vertices;
        public readonly Buffer indices = indices;
        public readonly DeviceMemory verticesMemory = verticesMemory;
        public readonly DeviceMemory indicesMemory = indicesMemory;
        public readonly uint indicesCount = indicesCount;
    }
}
