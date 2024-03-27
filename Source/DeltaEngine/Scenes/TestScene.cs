using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS;
using Delta.ECS.Components;
using Delta.Files.Defaults;
using Delta.Rendering;
using Delta.Scripting;
using System;
using System.Buffers;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Delta.Scenes;
internal static class TestScene
{
    public static Scene Scene => NewScene();

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

    private static Scene NewScene()
    {
        var scene = new Scene();
        InitWorldSimple(scene);
        scene.AddJob(new MoveTransformsJob(scene._world, scene.DeltaTime));
        scene.AddJob(new Renderer(scene._world, "TestScene"));
        scene.AddJob(new RemoveDirtyJob(scene._world));
        scene.AddJob(new FpsDropper(60, scene.DeltaTime));
        return scene;
    }

    private static void InitWorldSimple(Scene scene)
    {
        Transform defaultTransform = new() { Rotation = Quaternion.Identity, Scale = Vector3.One };
        for (int i = 0; i < N; i++)
            scene._world.Create(defaultTransform);
        var transforms = ArrayPool<Entity>.Shared.Rent(N);
        scene._world.GetEntities(new QueryDescription().WithAll<Transform>(), transforms);
        Render deltaRend = new()
        {
            Material = VCShader.VCMat,
            Mesh = DeltaMesh.Mesh
        };
        Render triangleRend = new()
        {
            Material = VCShader.VCMat,
            Mesh = TriangleMesh.Mesh
        };

        int deltaCount = 0;
        int triangleCount = 0;
        for (int i = 0; i < N; i++)
        {
            bool delta = rnd.NextSingle() > 0.5f;
            transforms[i].Add(delta ? deltaRend : triangleRend);
            transforms[i].Add(delta ?
            new MoveToTarget()
            {
                speed = 0.5f
            } :
            new MoveToTarget()
            {
                speed = 0.25f
            });
            deltaCount += delta ? 1 : 0;
            triangleCount += delta ? 0 : 1;
        }

        Console.WriteLine($"Delta count {deltaCount}");
        Console.WriteLine($"Triangle count {triangleCount}");

        ArrayPool<Entity>.Shared.Return(transforms);
        scene._world.TrimExcess();

        var tr = new QueryDescription().WithAll<Transform>();
        scene._world.Add<DirtyFlag<Transform>>(tr);
    }



    private readonly struct MoveTransformsJob(World world, Func<float> deltaTime) : ISystem
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
                m.percent += deltaTime * m.speed;
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

    [Component]
    private struct MoveToTarget
    {
        public Vector3 start;
        public Vector3 target;
        public float percent;
        public float speed;
        public float startScale;
        public float targetScale;
    }

    private readonly struct RemoveDirtyJob(World world) : ISystem
    {
        private readonly QueryDescription _dirtyTransforms = new QueryDescription().WithAll<DirtyFlag<Transform>>();
        private readonly QueryDescription _dirtyRenders = new QueryDescription().WithAll<DirtyFlag<Render>>();
        public void Execute()
        {
            world.Remove<DirtyFlag<Transform>>(_dirtyTransforms);
            world.Remove<DirtyFlag<Render>>(_dirtyRenders);
        }
    }

    private static readonly Random rnd = Random.Shared;

    private static Vector3 RndVector()
    {
        var xy = new Vector2(rnd.NextSingle() - 0.5f, rnd.NextSingle() - 0.5f);
        var position = new Vector3(xy.X, xy.Y, 0) * 2;
        return position;
    }


    private readonly struct FpsDropper(int targetFrameRate, Func<float> deltaTime) : ISystem
    {
        private readonly float _targetDeltaTime = 1f / (float)targetFrameRate;
        public void Execute()
        {
            var toSleep = _targetDeltaTime - deltaTime.Invoke();
            if (toSleep > 0f)
                Thread.Sleep(TimeSpan.FromSeconds(toSleep));
        }
    }
}