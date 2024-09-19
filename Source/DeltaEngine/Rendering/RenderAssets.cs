using Delta.Assets;
using Delta.Rendering.Headless;
using Delta.Rendering.Internal;
using Silk.NET.Vulkan;
using System;
using System.Collections.Generic;
using Buffer = Silk.NET.Vulkan.Buffer;


namespace Delta.Rendering;
internal class RenderAssets : IDisposable
{
    private readonly Dictionary<GuidAsset<ShaderData>, (Pipeline pipeline, VertexAttribute mask)> _renderToPipeline;
    private readonly Dictionary<GuidAsset<MeshData>, MeshHandler> _renderToMeshHandler;

    private readonly Vk _vk;
    private readonly DeviceQueues _deviceQ;
    private readonly PipelineLayout _pipelineLayout;
    private readonly RenderPass _renderPass;

    public RenderAssets(RenderBase renderBase)
    {
        _vk = renderBase.vk;
        _deviceQ = renderBase.deviceQ;
        _pipelineLayout = renderBase.pipelineLayout;
        _renderPass = renderBase.renderPass;
        _renderToPipeline = [];
        _renderToMeshHandler = [];
    }

    public (Pipeline pipeline, VertexAttribute mask) GetPipelineAndAttributes(GuidAsset<ShaderData> shader)
    {
        if (!_renderToPipeline.TryGetValue(shader, out var maskedPipeline))
        {
            var pipeline = RenderHelper.CreateGraphicsPipeline(_vk, _deviceQ, _pipelineLayout, _renderPass, shader, out var mask);
            _renderToPipeline[shader] = maskedPipeline = (pipeline, mask);
        }
        return maskedPipeline;
    }

    public (Buffer vertices, Buffer indices) GetVertexIndexBuffers(GuidAsset<MeshData> mesh, VertexAttribute vertexMask)
    {
        if (!_renderToMeshHandler.TryGetValue(mesh, out var meshHandler))
        {
            var meshAsset = mesh.GetAsset();
            var (vertexBuffer, vertexMemory) = RenderHelper.CreateVertexBuffer(_vk, _deviceQ, meshAsset, vertexMask);
            var (indexBuffer, indexMemory) = RenderHelper.CreateIndexBuffer(_vk, _deviceQ, meshAsset);
            _renderToMeshHandler[mesh] = meshHandler = new(vertexBuffer, vertexMemory, indexBuffer, indexMemory, meshAsset.GetIndicesCount());
        }
        return (meshHandler.vertices, meshHandler.indices);
    }

    public (Buffer vertices, Buffer indices, uint indicesCount) GetVertexIndexBuffersAndCount(GuidAsset<MeshData> mesh, VertexAttribute vertexMask)
    {
        if (!_renderToMeshHandler.TryGetValue(mesh, out var meshHandler))
        {
            var meshAsset = mesh.GetAsset();
            var (vertexBuffer, vertexMemory) = RenderHelper.CreateVertexBuffer(_vk, _deviceQ, meshAsset, vertexMask);
            var (indexBuffer, indexMemory) = RenderHelper.CreateIndexBuffer(_vk, _deviceQ, meshAsset);
            _renderToMeshHandler[mesh] = meshHandler = new(vertexBuffer, vertexMemory, indexBuffer, indexMemory, meshAsset.GetIndicesCount());
        }
        return (meshHandler.vertices, meshHandler.indices, meshHandler.indicesCount);
    }

    public void Dispose()
    {
        unsafe
        {
            foreach (var item in _renderToPipeline)
                _vk.DestroyPipeline(_deviceQ, item.Value.pipeline, null);
            foreach (var item in _renderToMeshHandler)
            {
                _vk.DestroyBuffer(_deviceQ, item.Value.vertices, null);
                _vk.DestroyBuffer(_deviceQ, item.Value.indices, null);
                _vk.FreeMemory(_deviceQ, item.Value.verticesMemory, null);
                _vk.FreeMemory(_deviceQ, item.Value.indicesMemory, null);
            }
        }
        _renderToPipeline.Clear();
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
