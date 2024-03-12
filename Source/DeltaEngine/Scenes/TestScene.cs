using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS;
using Delta.Files.Defaults;
using Delta.Rendering;
using JobScheduler;
using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Delta.Scenes;
internal static class TestScene
{
    public static Scene Scene { get; private set; }

    //private const int N = 1_000_000;
    //private const int N = 500_000;
    //private const int N = 300_000;
    //private const int N = 200_000;
    //private const int N = 100_000;
    //private const int N = 10_000;
    //private const int N = 1_000;
    //private const int N = 100;
    private const int N = 20;
    //private const int N = 10;
    //private const int N = 2;

    static TestScene()
    {
        Scene = new Scene();
        InitWorld();
        Scene.AddJob(new MoveTransformsJob(Scene._world, Scene.DeltaTime));
        Scene.AddJob(new Renderer(Scene._world, "TestScene"));
        Scene.AddJob(new RemoveDirtyJob(Scene._world));
        Scene.AddJob(new FpsDropper(Scene.DeltaTime));
    }

    private static void InitWorld()
    {
        Transform defaultTransform = new() { Rotation = Quaternion.Identity, Scale = Vector3.One };
        for (int i = 0; i < N; i++)
            Scene._world.Create(defaultTransform);
        var transforms = ArrayPool<Entity>.Shared.Rent(N);
        Scene._world.GetEntities(new QueryDescription().WithAll<Transform>(), transforms);
        Render rend1 = new()
        {
            Material = VCShader.VCMat,
            Mesh = DeltaMesh.Mesh
        };
        Render rend2 = new()
        {
            Material = VCShader.VCMat,
            Mesh = TriangleMesh.Mesh
        };
        for (int i = 0; i < N / 2; i++)
        {
            transforms[i].Add(new ChildOf(Scene._world.Reference(transforms[i + (N / 2)])));
            transforms[i].Add(rnd.NextSingle() > 0.5f ? rend1 : rend2);
        }
        ArrayPool<Entity>.Shared.Return(transforms);
        Scene._world.TrimExcess();

        var tr = new QueryDescription().WithAll<Transform>();
        Scene._world.Query(tr, (ref Transform t) => t.Scale = new(1));
        Scene._world.Query(tr, (ref Transform t) => t.Position = RndVector());
        Scene._world.Add<DirtyFlag<Transform>>(tr);

        var move = new QueryDescription().WithAll<Transform>().WithNone<ChildOf>();
        Scene._world.Add<MoveToTarget>(move);
        move = new QueryDescription().WithAll<Transform, ChildOf>();
        Scene._world.Query(move, (ref Transform t) => t.Position = new(0, 0.5f, 0));
    }


    private readonly struct MoveTransformsJob(World world, Func<float> deltaTime) : IJob
    {
        private readonly World _sceneWorld = world;
        public readonly void Execute()
        {
            var query = new QueryDescription().WithAll<Transform, MoveToTarget>();
            Move move = new(deltaTime.Invoke());
            _sceneWorld.InlineDirtyQuery<Move, Transform, MoveToTarget>(query, ref move);
        }

        private readonly struct Move(float deltaTime) : IForEach<Transform, MoveToTarget>
        {
            private readonly float deltaTime = deltaTime;

            [MethodImpl(Inl)]
            public readonly void Update(ref Transform t, ref MoveToTarget m)
            {
                t.Position = Vector3.Lerp(m.start, m.target, m.percent);
                t.Scale = new(float.Lerp(m.startScale, m.targetScale, m.percent));
                m.percent += deltaTime * 0.5f;
                m.percent = Math.Clamp(m.percent, 0f, 1f);
                t.Rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * deltaTime * 3f);
                if (m.percent == 1)
                {
                    m.start = m.target;
                    m.target = RndVector();
                    m.startScale = m.targetScale;
                    m.targetScale = rnd.NextSingle() * 0.1f;
                    m.percent = 0;
                }
            }
        }
    }

    public struct MoveToTarget
    {
        public Vector3 start;
        public Vector3 target;
        public float percent;
        public float startScale;
        public float targetScale;
    }

    private readonly struct RemoveDirtyJob(World world) : IJob
    {
        private readonly QueryDescription _dirtyTransforms = new QueryDescription().WithAll<DirtyFlag<Transform>>();
        private readonly QueryDescription _dirtyRenders = new QueryDescription().WithAll<DirtyFlag<Render>>();
        public void Execute()
        {
            world.Remove<DirtyFlag<Transform>>(_dirtyTransforms);
            world.Remove<DirtyFlag<Render>>(_dirtyRenders);
        }
    }
    static readonly Random rnd = new(42);

    private static Vector3 RndVector()
    {
        var xy = new Vector2(rnd.NextSingle() - 0.5f, rnd.NextSingle() - 0.5f);
        var position = new Vector3(xy.X, xy.Y, 0) * 2;
        return position;
    }


    private readonly struct FpsDropper(Func<float> deltaTime) : IJob
    {
        const float TargetDeltaTime = 1f / 15f;
        public void Execute()
        {
            var toSleep = TargetDeltaTime - deltaTime.Invoke();
            if (toSleep > 0f)
                Thread.Sleep(TimeSpan.FromSeconds(toSleep));
        }
    }
}