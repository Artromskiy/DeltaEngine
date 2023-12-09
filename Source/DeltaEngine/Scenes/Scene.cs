using Arch.Core;
using DeltaEngine.ECS;
using DeltaEngine.Rendering;
using System.Numerics;

namespace DeltaEngine.Scenes;
internal class Scene
{
    private readonly GpuMappedSystem<Transform, Matrix4x4> _transformSystem;
    private readonly Renderer renderer;
    private readonly World _sceneWorld;
    private readonly int N = 10000;

    public Scene(World world)
    {
        _sceneWorld = world;
        for (int i = 0; i < N; i++)
            world.Create<Transform>();

        renderer = new Renderer("Delta Engine");
        _transformSystem = new GpuMappedSystem<Transform, Matrix4x4>(_sceneWorld, new TransformMapper(), renderer._rendererData);
    }

    private void Start()
    {

    }

    public void Run()
    {

    }


    private struct TransformMapper : IGpuMapper<Transform, Matrix4x4>
    {
        public readonly Matrix4x4 Map(Transform from) => from.LocalMatrix;
    }

}
