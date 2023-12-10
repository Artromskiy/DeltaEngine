using Arch.Core;
using DeltaEngine.Collections;
using DeltaEngine.ECS;
using DeltaEngine.Rendering;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DeltaEngine.Scenes;
internal class Scene
{
    private readonly GpuMappedSystem<Transform, TRSData> _transformSystem;
    private readonly Renderer _renderer;
    private readonly World _sceneWorld;

    public Scene(World world, Renderer renderer)
    {
        _sceneWorld = world;
        new JobScheduler.JobScheduler("WorkerThread");
        _sceneWorld.Add<MoveTo>(new QueryDescription().WithAll<Transform>());
        _renderer = renderer;
        _transformSystem = new GpuMappedSystem<Transform, TRSData>(_sceneWorld, new TransformMapper(), _renderer._rendererData);
        Dirt<Transform>();
    }

    public struct MoveTo
    {
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
        public float percent;
    }

    [MethodImpl(Inl)]
    public void Run()
    {
        //var query = new QueryDescription().WithAll<Transform, MoveTo>();
        //_sceneWorld.InlineQuery<MoveTransforms, Transform, MoveTo>(query);
        //Dirt<Transform>();
        _transformSystem.UpdateDirty();
        //Clear<Transform>();
    }

    private struct MoveTransforms : IForEach<Transform, MoveTo>
    {
        public readonly void Update(ref Transform t, ref MoveTo m)
        {
            if (m.percent == 1)
            {
                m.position = new Vector3((Random.Shared.NextSingle() - 0.5f) * 100);
                m.scale = new Vector3(Random.Shared.NextSingle() * 10f);
                m.rotation = Quaternion.CreateFromYawPitchRoll(Random.Shared.NextSingle() * MathF.PI, Random.Shared.NextSingle() * MathF.PI, Random.Shared.NextSingle() * MathF.PI);
                m.percent = 0;
            }
            t.Position = Vector3.Lerp(t.Position, m.position, m.percent);
            t.Scale = Vector3.Lerp(t.Scale, m.scale, m.percent);
            t.Rotation = Quaternion.Slerp(t.Rotation, m.rotation, m.percent);
            m.percent += 16f / 1000f;
            m.percent = Math.Clamp(m.percent, 0f, 1f);
        }
    }

    [MethodImpl(Inl)]
    private void Clear<T>() where T : IDirty
    {
        var queryDesc = new QueryDescription().WithAll<T>();
        Clearer<T> clearer = new();
        _sceneWorld.InlineParallelQuery<Clearer<T>, T>(queryDesc, ref clearer);
    }
    [MethodImpl(Inl)]
    private void Dirt<T>() where T : IDirty
    {
        var queryDesc = new QueryDescription().WithAll<T>();
        Dirter<T> dirter = new();
        _sceneWorld.InlineParallelQuery<Dirter<T>, T>(queryDesc, ref dirter);
    }

    private readonly struct Clearer<T> : IForEach<T> where T : IDirty
    {
        [MethodImpl(Inl)]
        public readonly void Update(ref T component) => component.IsDirty = false;
    }
    private readonly struct Dirter<T> : IForEach<T> where T : IDirty
    {
        [MethodImpl(Inl)]
        public readonly void Update(ref T component) => component.IsDirty = true;
    }


    private struct TransformMapper : IGpuMapper<Transform, TRSData>
    {
        [MethodImpl(Inl)]
        public readonly TRSData Map(ref Transform from) => new() 
        {
            position = new(from.Position, 0),
            rotation = from.Rotation,
            scale = new(from.Scale, 0),
        };
    }

    private struct TRSData
    {
        public Vector4 position;
        public Quaternion rotation;
        public Vector4 scale;
    }
}