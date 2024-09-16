using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS;
using Delta.ECS.Attributes;
using Delta.ECS.Components;
using Delta.Rendering;
using Delta.Runtime;
using Delta.Scenes;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Numerics;
using System.Threading;

namespace Delta.Files.Defaults;
internal static class TestScene
{
    public static Scene Scene => NewScene();

    //private const int N = 1_000_000;
    //private const int N = 500_000;
    //private const int N = 300_000;
    //private const int N = 200_000;
    //private const int N = 100_000;
    //private const int N = 10_000;
    //private const int N = 5_000; // TODO Check crush
    //private const int N = 1_000;
    //private const int N = 100;
    //private const int N = 20;
    private const int N = 10;
    //private const int N = 2;

    private static Scene NewScene()
    {
        var scene = new Scene();
        InitWorldSimple(scene);
        var graphics = IRuntimeContext.Current.GraphicsModule;
        if (graphics is not DummyGraphics)
            graphics.AddRenderBatcher(new SceneBatcher());
        scene.AddSystem(new MoveTransformsJob(scene._world, scene.DeltaTime));
        //scene.AddJob(new RemoveDirtyJob(scene._world));
        //scene.AddJob(new FpsDropper(60, scene.DeltaTime));

        scene._world.TrimExcess();
        GC.Collect();
        return scene;
    }

    private static void InitWorldSimple(Scene scene)
    {
        CreateManyTriangles(scene, N);
        //CreateOneDelta(scene);
        CreateCamera(scene);
        CreateEmptyTestComponent(scene);
        CreateFullTestComponent(scene);
        MarkTransformsDirty(scene);
    }

    private static void CreateCamera(Scene scene)
    {
        var cameraEntity = scene.AddEntity().Entity;
        cameraEntity.Add<Transform>(new()
        {
            position = new Vector3(0, 0, -2),
            rotation = Quaternion.Identity,
            scale = Vector3.One
        });
        cameraEntity.Add<Camera>(new()
        {
            fieldOfView = 90,
            aspectRation = 1,
            nearPlaneDistance = float.Epsilon,
            farPlaneDistance = 1000
        });
        cameraEntity.Add<EntityName>(new("Camera"));
    }

    private static void CreateEmptyTestComponent(Scene scene)
    {
        var testEntity = scene.AddEntity().Entity;
        testEntity.Add<EntityName>(new("Empty"));
        testEntity.Add<TestArraysAndSoOn>(new());
    }

    private static void CreateFullTestComponent(Scene scene)
    {
        var testEntity = scene.AddEntity().Entity;
        testEntity.Add<EntityName>(new("Full"));
        testEntity.Add<TestArraysAndSoOn>(new()
        {
            stringsDictionary = [],
            stringsList = [],
            stringsSet = []
        });
        testEntity.Add<CompStruct>();
    }

    private static void MarkTransformsDirty(Scene scene)
    {
        var tr = new QueryDescription().WithAll<Transform>();
        scene._world.Add<DirtyFlag<Transform>>(tr);
    }

    private static void CreateManyTriangles(Scene scene, int count)
    {
        Transform defaultTransform = new() { rotation = Quaternion.Identity, scale = Vector3.One };
        for (int i = 0; i < count; i++)
        {
            scene.AddEntity().Entity.Add(defaultTransform);
        }
        var transforms = ArrayPool<Entity>.Shared.Rent(count);
        scene._world.GetEntities(new QueryDescription().WithAll<Transform>(), transforms);
        var material = VCShader.VCMat;
        Render deltaRend = new()
        {
            Material = material,
            mesh = DeltaMesh.Mesh
        };
        Render triangleRend = new()
        {
            Material = material,
            mesh = TriangleMesh.Mesh
        };

        int deltaCount = 0;
        int triangleCount = 0;
        for (int i = 0; i < count; i++)
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
    }

    private static void CreateOneDelta(Scene scene)
    {
        var entity = scene.AddEntity().Entity;
        Transform defaultTransform = new()
        {
            scale = Vector3.One * 1f,
            rotation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * 0.5f),
            position = new Vector3(0.5f, 0, 10)
        };
        Render deltaRend = new() { Material = VCShader.VCMat, mesh = DeltaMesh.Mesh };
        entity.Add(defaultTransform);
        entity.Add(deltaRend);
    }


    private readonly struct MoveTransformsJob(World world, Func<float> deltaTime) : ISystem
    {
        private readonly World _sceneWorld = world;
        public readonly void Execute()
        {
            var query = new QueryDescription().WithAll<Transform, MoveToTarget>();
            Move move = new(deltaTime.Invoke());
            var count = _sceneWorld.CountEntities(query);
            _sceneWorld.InlineDirtyQuery<Move, Transform, MoveToTarget>(query, ref move);
        }

        private readonly struct Move(float deltaTime) : IForEach<Transform, MoveToTarget>
        {
            private readonly float deltaTime = deltaTime;

            [Imp(Inl)]
            public readonly void Update(ref Transform t, ref MoveToTarget m)
            {
                t.position = Vector3.Lerp(m.start, m.target, m.percent);
                t.scale = new(float.Lerp(m.startScale, m.targetScale, m.percent));
                m.percent += deltaTime * m.speed;
                m.percent = Math.Clamp(m.percent, 0f, 1f);
                t.rotation *= Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathF.PI * deltaTime * 3f);
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
        private readonly float _targetDeltaTime = 1f / targetFrameRate;
        public void Execute()
        {
            var toSleep = _targetDeltaTime - deltaTime.Invoke();
            if (toSleep > 0f)
                Thread.Sleep(TimeSpan.FromSeconds(toSleep));
        }
    }
}

[Component]
public struct MoveToTarget
{
    public Vector3 start;
    public Vector3 target;
    public float percent;
    public float speed;
    public float startScale;
    public float targetScale;
}

[Component]
public struct TestArraysAndSoOn
{
    //public string[] strings;
    public List<string> stringsList;
    public HashSet<string> stringsSet;
    public Dictionary<string, string> stringsDictionary;
}
[Component]
public struct BigCompStruct
{
    public float vec1;
    public float vec2;
    public float vec3;
    public float asdvec1;
    public float vecasd1;
    public float veca1;
    public float vesdc1;
    public float vecaasd1;
    public float veaaca1;
    public float vesssc1;
    public float vecs1;
    public float vedc1;
    public float veddc1;
    public float vecdd1;
    public float vecddd1;
    public float vec1d;
    public float avec1;
    public float vaaec1;
    public float vecad1;
    public float vasec1;
    public float vecass1;
    public float veasc1;
    public float vecas1;
    public float vedasc1;
    public float veac1;
    public float veddac1;
    public float vedsc1;
    public float vedsac1;
    public float veddadc1;
    public float vecsas1;
    public float vasdec1;
    public float vecasda1;
    public float veasdc1;
    public float vedfc1;
    public float vfdfgec1;
    public float vecdfg1;
    public float vegfc1;
    public float vgec1;
    public float vefc1;
    public float vecf1;
    public float vecg1;
    public float vecd1;
    public float vedgfc1;
    public float vecgg1;
    public float vegc1;
    public float veffc1;
    public float veggc1;
    public float ggvec1;
    public float gvec1;
    public float gggvec1;
    public float fvec1;
    public float vdgec1;
    public float vdec1;

}

[Component]
public struct CompStruct
{
    public float vec1;
    public Vector2 vec2;
    public Vector3 vec3;
    public Vector3 vec4;
    public Quaternion vec5;
    public Vector4 vec6;
    public Matrix4x4 vec7;
    public CompStruct1 vec8;
}

[Component]
public struct CompStruct1
{
    public Vector3 vec3;
    public Vector3 vec4;
    public CompStruct2 vec5;
    public Matrix4x4 vec6;
    public CompStruct3 vec7;
}
[Component]
public struct CompStruct2
{
    public float vec1;
    public Vector2 vec2;
    public Vector3 vec3;
    public Vector3 vec4;
    public Quaternion vec5;
    public Matrix4x4 vec6;
    public CompStruct3 vec7;
}
[Component]
public struct CompStruct3
{
    public float vec1;
    public Vector2 vec2;
    public Vector3 vec3;
    public Vector3 vec4;
    public Quaternion vec5;
    public Matrix4x4 vec6;
}

