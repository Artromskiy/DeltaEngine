using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Rendering.Internal;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Delta.Rendering;
internal class SceneDataProvider : ISystem
{
    private readonly World _world;

    private readonly ISystem[] systems;

    public readonly GpuArray<GpuCameraData> camera;

    private static readonly QueryDescription _cameraDescription = new QueryDescription().WithAll<Camera>();

    public SceneDataProvider(World world, RenderBase renderBase)
    {
        _world = world;
        camera = new GpuArray<GpuCameraData>(renderBase, 1);
        systems =
        [
             new WriteCamera(_world, camera),
        ];
    }

    public void Execute()
    {
        foreach (var item in systems)
            using (item as IDisposable)
                item.Execute();
    }

    private readonly struct WriteCamera(World world, GpuArray<GpuCameraData> _cameraArray) : ISystem
    {
        public void Execute()
        {
            var writer = _cameraArray.GetWriter();
            if (world.CountEntities(_cameraDescription) != 0)
                world.Query(_cameraDescription, (entity) => writer[0] = GetCameraData(entity));
            else
                writer[0] = DefaultCameraData();
        }
    }

    [MethodImpl(Inl)]
    private static GpuCameraData GetCameraData(Entity entity)
    {
        var matrix = entity.GetWorldMatrix();
        var camera = entity.Get<Camera>();
        Matrix4x4.Decompose(matrix, out var _, out var rotation, out var position);
        var fwd = Vector3.Transform(Vector3.UnitZ, rotation);
        var up = Vector3.Transform(Vector3.UnitY, rotation);
        var view = Matrix4x4.CreateLookToLeftHanded(position, fwd, up);
        return new GpuCameraData()
        {
            position = new(position, 0),
            rotation = Quaternion.Identity,
            proj = camera.projection,
            view = view,
            projView = Matrix4x4.Multiply(view, camera.projection) // inverted order, as vulkan/opengl uses other memory layout for matrices
        };
    }

    private static GpuCameraData DefaultCameraData() => new()
    {
        position = default,
        rotation = Quaternion.Identity,
        proj = Matrix4x4.Identity,
        view = Matrix4x4.Identity,
        projView = Matrix4x4.Identity
    };
}
