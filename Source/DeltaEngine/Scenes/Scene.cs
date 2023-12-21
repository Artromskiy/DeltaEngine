using Arch.Core;
using Delta.ECS;
using Delta.Rendering;
using JobScheduler;
using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Delta.Scenes;
internal class Scene
{
    private readonly Renderer _renderer;
    private readonly World _sceneWorld;
    public Scene(World world, Renderer renderer)
    {
        _sceneWorld = world;
        var tr = new QueryDescription().WithAll<Transform>();
        _sceneWorld.Query(tr, (ref Transform t) => t.Scale = new(0.1f));
        _sceneWorld.Query(tr, (ref Transform t) => t.Position = new Vector3(0, -0.25f, 0));
        _sceneWorld.Add<DirtyFlag<Transform>>(tr);

        //var move = new QueryDescription().WithAll<Transform>().WithNone<ChildOf>();
        //int count = _sceneWorld.CountEntities(move);
        //// move parents
        //_sceneWorld.Query(move, (Entity e) =>
        //{
        //    if (count <= 0)
        //        return;
        //    count--;
        //    _sceneWorld.Add<MoveToTarget>(e);
        //});
        _renderer = renderer;

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

        //_sceneWorld.Remove<DirtyFlag<Transform>>(new QueryDescription().WithAll<DirtyFlag<Transform>>());

        _sceneSw.Start();
        //var h1 = JobScheduler.JobScheduler.Instance.Schedule(new MoveAllTransforms(_sceneWorld, deltaTime));
        var h2 = JobScheduler.JobScheduler.Instance.Schedule(_renderer);
        JobScheduler.JobScheduler.Instance.Flush();

        //h1.Complete();
        h2.Complete();

        _sceneSw.Stop();
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
        var xy = new Vector2(rnd.NextSingle() - 0.5f, rnd.NextSingle() - 0.5f) * 0.5f;
        xy = xy.Length() > 0.25f ? Vector2.Normalize(xy) * 0.25f : xy;
        var position = new Vector3(xy.X, xy.Y, 0) * 2;
        return position;
    }


    /*
    [0][1][2][3][4][5][6][7][0][1][2][3][4][5][6][7][0][1][2][3][4][5][6][7][0][1]

    [t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t]  transforms
    [i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i]  parent index

    [r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r]  render
    [i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i]  reference to transform


    BitArray
    [0][0][0][0][0][0][0][0][x][0][0][0][x][0][0][0][0][0][0][0][x][0][0][0][0][0] dirty transforms at host (1 world step) // updates of frame are done with mapped system class, but we must store bitarray during update
    [0][0][0][0][0][x][0][0][0][0][0][0][0][0][0][0][x][0][0][0][0][0][0][0][0][0] dirty transforms at host (2 world step)
    [0][0][x][0][0][0][0][0][x][0][x][0][x][0][0][0][0][x][0][0][0][x][0][0][0][0] dirty transforms at host (3 world step)
                                          ____
                                       __|    |__
                                      \         /
                                       \       /
                                        \     /
                                         \   /
                                          \ /
                                           *
    [0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0][0] dirty transforms after frame rendered (none, as always update all before render) // BAD as it leads to increase of work submitted to each frame buffer
    [0][0][0][0][0][0][0][0][x][0][0][0][x][0][0][0][0][0][0][0][x][0][0][0][0][0] dirty transforms at frame (1 world step) (bitwice or with host)
    [0][0][0][0][0][x][0][0][x][0][0][0][x][0][0][0][x][0][0][0][x][0][0][0][0][0] dirty transforms at frame (2 world step) (bitwice or with host)
    [0][0][x][0][0][x][0][0][x][0][x][0][x][0][0][0][x][x][0][0][x][x][0][0][0][0] dirty transforms at frame (2 world step) (bitwice or with host)

                                                                                   same logic applied for each replicated buffer

                                                                                   lets say system asks to render current frame

    [0][1][2][3][4][5][6][7][0][1][2][3][4][5][6][7][0][1][2][3][4][5][6][7][0][1]
    [2][5][8][10][12][16][17][20][21]                                              indices to copy from host to buffer constructed from dirty mask

                                                                                   setup compute shader for data replication

    [t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t] host transforms
    [t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t][t] frame transforms
    [2][5][8][10][12][16][17][20][21]                                              replication indices         Change of any index leads to global position recalculation via tree

    [i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i] host parent of transform Index
    [i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i] frame parent of trnasform Index
    [2][5][8][10][12][16][17][20][21]                                              replication indices         Change of any index leads to global position recalculation via tree
                                                                                                               
                                                                                                               We also can build array of changed layers (transform changed - whole tree dirty, parenting changed - whole tree dirty)
                                                                                                               This will give ability to correctly calculate world position matrices for whole array using N steps of compute shader where N is depth of tree
                                                                                                               if we moved root we will send N indices to gpu it's N uints => N * 4 bytes => N * 4 bytes * 120 fps => 480 MB/second (so it fits in transfer bottleneck)

                                                                                                               The question is where we should run compute shader for global transform recalculations?
                                                                                                               Running it in frame buffer will drop performance by count of buffers. Running on local leads to slower compute shader
                                                                                                               We can have device global compute layer which will take care of transfering data and it's preparing for frame buffer

    [r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r] host renders                We don't neeed to send whole render data, we can send an index of transform inside frame buffer and it leads to somehow grouped transform indices
    [r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r][r] frame renders
    [2][5][8][10][12][16][17][20][21]                                              replication indices

    [i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i] host parent of render Index
    [i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i][i] frame parent of render Index
    [2][5][8][10][12][16][17][20][21]                                              replication indices


    So before rendering frame each Render asks if it has dirty transform in parents;
    for each render with dirty transfrom in parent we go up to point where dirtiness ends (exclusive)
    we store unqieue versId of dirty transforms based on their hierarchy depth in array,                        Array I
    0 depth transforms will be stored first and leafs will be stored last
    we store count of transforms with same depth in second array (0 depth count first, leaf depth last)         Array L
    we store count of dirty depths                                                                              Uint  N


    we send this data to compute shader which will do something like this:

    void RebuildWorld()
    {
        for(int l = 0; l < N; l++)  // Execute for each layer
        {
            int indices = L[l];     // Count of transforms in layer

            for(int i = 0; i < indices; i++) // Execute same layer transforms
            {
                int index = I[i];
                world[index] = local[index] * world[parent[index]];
            }

            barrier();      //  wait for layer calculation end
        }
    }
    With this approach we can also update bounding boxes etc

    After that all selected versIds marked clear (not all, just related, as other could be bound to render in later frames)

    */
}