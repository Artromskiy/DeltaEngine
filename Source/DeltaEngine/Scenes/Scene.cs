using Arch.Core;
using DeltaEngine.ECS;
using DeltaEngine.Rendering;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DeltaEngine.Scenes;
internal class Scene
{
    private readonly Renderer _renderer;
    private readonly World _sceneWorld;
    private readonly JobScheduler.JobScheduler? _jobScheduler;
    public Scene(World world, Renderer renderer)
    {
        _sceneWorld = world;
        _jobScheduler = new JobScheduler.JobScheduler("WorkerThread");
        int count = _sceneWorld.CountEntities(new QueryDescription().WithAll<Transform>());
        _sceneWorld.Query(new QueryDescription().WithAll<Transform>(), (Entity e) =>
        {
            if (count <= 0)
                return;
            count--;
            _sceneWorld.Add(e, new MoveToTarget()
            {
                percent = 1,
                target = RndVector(),
                targetScale = 0.1f
            });
        });
        _renderer = renderer;
        _sceneWorld.Query(new QueryDescription().WithAll<Transform>(), (ref Transform t) => t.Scale = new(0.1f));
        _sceneWorld.Query(new QueryDescription().WithAll<Transform>(), (ref Transform t) => t.Position = RndVector());
    }

    private readonly Stopwatch _sceneSw = new();
    public TimeSpan GetSceneMetric => _sceneSw.Elapsed;
    public void ClearSceneMetric() => _sceneSw.Reset();

    public struct MoveToTarget
    {
        public Vector3 start;
        public Vector3 target;
        public float percent;
        public float startScale;
        public float targetScale;
    }

    [MethodImpl(NoInl)]
    public void Run(float deltaTime)
    {
        _renderer.Sync();
        var clearer = new Clearer<IDirty>();
        _sceneWorld.InlineParallelQuery<Clearer<IDirty>, IDirty>(new QueryDescription().WithAll<IDirty>(), ref clearer);
        var t1 = Task.Run(_renderer.Run);
        var t2 = Task.Run(() =>
        {
            _sceneSw.Start();
            var query = new QueryDescription().WithAll<Transform, MoveToTarget>();
            MoveTransforms move = new(deltaTime);
            _sceneWorld.InlineParallelQuery<MoveTransforms, Transform, MoveToTarget>(query, ref move);
            _sceneSw.Stop();
        });
        Task.WaitAll(t1, t2);
    }


    private readonly struct MoveTransforms(float deltaTime) : IForEach<Transform, MoveToTarget>
    {
        private readonly float deltaTime = deltaTime;

        [MethodImpl(Inl)]
        public readonly void Update(ref Transform t, ref MoveToTarget m)
        {
            var percent = 1 - InCubic(1 - m.percent);
            t.Position = Vector3.Lerp(m.start, m.target, percent);
            t.Scale = new(float.Lerp(m.startScale, m.targetScale, percent));
            m.percent += deltaTime * 0.5f;
            m.percent = Math.Clamp(m.percent, 0f, 1f);
            if (m.percent == 1)
            {
                m.start = m.target;
                m.target = RndVector();
                m.startScale = m.targetScale;
                m.targetScale = Random.Shared.NextSingle() * 0.1f;
                m.percent = 0;
            }
        }
    }

    [MethodImpl(Inl)]
    public static float InCubic(float t) => t * t * t;

    [MethodImpl(Inl)]
    public static Vector3 MoveTo(Vector3 src, Vector3 trg, float t)
    {
        var delta = trg - src;
        float d = delta.LengthSquared();
        return d <= t * t ? trg : src + (delta / MathF.Sqrt(d) * t);
    }

    private static Vector3 RndVector()
    {
        Random rnd = Random.Shared;
        var xy = new Vector2(rnd.NextSingle() - 0.5f, rnd.NextSingle() - 0.5f);
        xy = Vector2.Normalize(xy) * 0.5f;
        var position = new Vector3(xy.X, xy.Y, rnd.NextSingle() * 0.5f) * 2;
        return position;
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
}