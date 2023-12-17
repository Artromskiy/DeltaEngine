using Arch.CommandBuffer;
using Arch.Core;
using DeltaEngine.ECS;
using DeltaEngine.Rendering;
using JobScheduler;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DeltaEngine.Scenes;
internal class Scene
{
    private readonly Renderer _renderer;
    private readonly World _sceneWorld;
    private readonly JobScheduler.JobScheduler _jobScheduler;
    public Scene(World world, Renderer renderer)
    {
        _sceneWorld = world;
        _jobScheduler = new JobScheduler.JobScheduler("WorkerThread");
        //var buffer = new Arch.CommandBuffer.CommandBuffer(_sceneWorld);
        int count = _sceneWorld.CountEntities(new QueryDescription().WithAll<Transform>())/10;
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

        CommandBuffer v = new(_sceneWorld);

        var dirty = _sceneWorld.CountEntities(new QueryDescription().WithAll<DirtyFlag<Transform>>());
        _sceneWorld.Remove<DirtyFlag<Transform>>(new QueryDescription().WithAll<DirtyFlag<Transform>>());
        dirty = _sceneWorld.CountEntities(new QueryDescription().WithAll<DirtyFlag<Transform>>());

        _sceneSw.Start();
        var move = new MoveAllTransforms(_sceneWorld, deltaTime);
        move.Execute();
        _sceneSw.Stop();
        _renderer.Execute();
        //var h1 = _jobScheduler.Schedule(move);
        //var h2 = _jobScheduler.Schedule(_renderer);
        //_jobScheduler.Flush();

        //h1.Complete();
        //h2.Complete();
    }

    private readonly struct MoveAllTransforms(World world, float deltaTime) : IJob
    {
        private readonly World _sceneWorld = world;
        private readonly float _deltaTime = deltaTime;
        public readonly void Execute()
        {
            var query = new QueryDescription().WithAll<Transform, MoveToTarget>();
            MoveTransforms move = new(_deltaTime);
            _sceneWorld.InlineDirtyParallelQuery<MoveTransforms, Transform, MoveToTarget>(query, ref move);
        }
    }

    private readonly struct MoveTransforms(float deltaTime) : IForEach<Transform, MoveToTarget>
    {
        private readonly float deltaTime = deltaTime;

        [MethodImpl(Inl)]
        public readonly void Update(ref Transform t, ref MoveToTarget m)
        {
            var tpercent = 1 - InCubic(1 - m.percent);
            var spercent = InCubic(m.percent);
            t.Position = Vector3.Lerp(m.start, m.target, tpercent);
            t.Scale = new(float.Lerp(m.startScale, m.targetScale, spercent));
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
        xy = xy.Length() > 0.5f ? Vector2.Normalize(xy) * 0.5f : xy;
        var position = new Vector3(xy.X, xy.Y, rnd.NextSingle() * 0.5f) * 2;
        return position;
    }
}