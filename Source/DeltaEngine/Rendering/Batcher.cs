using CommunityToolkit.HighPerformance.Helpers;
using DeltaEngine.ECS;
using DeltaEngine.Files;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace DeltaEngine.Rendering;

public class Batcher
{
    private static bool log = false;

    private const string bindingPipeline = "Binding pipeline bound to shader";
    private const string bindingMaterial = "Binding material data to pipeline";
    private const string groupByMaterial = "Grouping meshes with same material";
    private const string instancingBegin = "Instancing draw stage begin";
    private const string staticInstancing = "Static objects with Instancing drawing";
    private const string dynamicInstancing = "Dynamic objects with Instancing drawing";
    private const string instancingEnd = "Instancing draw stage end";
    private const string batchingBegin = "Batching draw stage begin";
    private const string staticBatching = "Static objects with Batching drawing";
    private const string dynamicBatching = "Dynamic objects with Batching drawing";
    private const string batchingEnd = "Batching draw stage end";

    private readonly SortedSet<RenderData> set;

    private readonly HashSet<int> _usedEntities;


    // The better solution is to use compute shader
    // which will automatically take array of transform and indicies
    // and then assign it's non empty values to new array
    // Also if group contains just one instance object (because of unique mesh)
    // we need to somehow fallback to batching. The question is HOW?
    // If transform is static and mesh is small and unique
    // we can create batching _buffer per material

    private readonly Dictionary<GuidAsset<MaterialData>, MeshGroups> _renderGroups = new(); // for render data updates
    private readonly SortedDictionary<GuidAsset<MaterialData>, MeshGroups> _sortedRenderGroups = new(); // for rendering iteration


    private class MaterialDataCmparer : IComparer<GuidAsset<MaterialData>>
    {
        public int Compare(GuidAsset<MaterialData> x, GuidAsset<MaterialData> other)
        {
            var matDiff = x.guid.CompareTo(other.guid);
            if (matDiff == 0)
                return 0;
            var shaderDiff = x.Asset.shader.guid.CompareTo(other.Asset.shader.guid);
            if (shaderDiff != 0)
                return shaderDiff;
            return matDiff;
        }
    }

    public Batcher(RenderData[] prepared)
    {
        _usedEntities = new();
        set = new SortedSet<RenderData>(prepared);
        _renderGroups = new();
        _sortedRenderGroups = new(new MaterialDataCmparer());
        foreach (ref var data in new Span<RenderData>(prepared))
            Add(ref data);
    }

    private void Add(ref RenderData data)
    {
        Debug.Assert(_usedEntities.Add(data.id));
        if (!_renderGroups.TryGetValue(data.material, out var renderGroup))
        {
            _renderGroups[data.material] = renderGroup = new();
            _sortedRenderGroups.Add(data.material, renderGroup);
        }
        renderGroup.Add(ref data);
    }

    private void Remove(ref RenderData data)
    {
        Debug.Assert(_usedEntities.Remove(data.id));
        _renderGroups[data.material].Remove(ref data);
    }

    public void Change(ref RenderData oldOne, ref RenderData newOne)
    {
        Remove(ref oldOne);
        Add(ref newOne);
    }

    public static RenderData IterateWithCopy(RenderData[] data)
    {
        RenderData def = default;
        foreach (var item in data)
            def = item;
        return def;
    }

    public static void Draw3(RenderData[] data)
    {
        Console.WriteLine("ForEach");
        //Parallel.ForEach(data, item =>
        //{
        //    if (item.transformDirty)
        //        item.bindedGroup.Update(item.renderGroupId, item.transform);
        //});
    }

    public void Draw2()
    {
        int materialSwitches = 0;
        int shaderSwitches = 0;
        var currentShader = Guid.Empty;
        var currentMaterial = Guid.Empty;
        foreach (var item in set)
        {
            var shader = item.material.Asset.shader.guid;
            var material = item.material.guid;
            if (shader != currentShader)
            {
                Log(bindingPipeline);
                currentShader = shader;
                shaderSwitches++;
            }
            if (material != currentMaterial)
            {
                Log(bindingMaterial);
                currentMaterial = material;
                materialSwitches++;
            }
        }
    }


    /// <summary>
    /// Draft drawing method
    /// </summary>
    /// <param name="data"></param>
    public static void DrawOld(RenderData[] data, bool summaryLog, bool fullLog = false)
    {
        log = fullLog;

        int materialSwitches = 0;
        int shaderSwitches = 0;

        int staticInstancingCalls = 0;
        int staticBatchingCalls = 0;
        int dynamicInstancingCalls = 0;
        int dynamicBatchingCalls = 0;

        int staticInstanced = 0;
        int staticBatched = 0;
        int dynamicInstanced = 0;
        int dynamicBatched = 0;

        var materialGroups = data.GetGroupList(x => x.material);
        materialGroups.Sort((x1, x2) =>
        {
            var s1 = x1[0].material.Asset.shader;
            var s2 = x2[0].material.Asset.shader;
            return s1.guid.CompareTo(s2.guid);
        });
        var currentShader = Guid.Empty;
        foreach (var materialGroup in materialGroups)
        {
            var groupdShaderId = materialGroup[0].material.Asset.shader.guid;
            if (groupdShaderId != currentShader)
            {
                Log(bindingPipeline);
                currentShader = groupdShaderId;
                shaderSwitches++;
            }
            Log(bindingMaterial);
            materialSwitches++;

            Log(groupByMaterial);
            var meshGroups = materialGroup.GetGroup(x => x.mesh);

            List<RenderData> staticBatchGroup = new();
            List<RenderData> dynamicBatchGroup = new();

            Log(instancingBegin);
            foreach (var meshGroup in meshGroups)
            {
                var instancingGroups = meshGroup.Value.GetGroup(x => x.isStatic);

                if (instancingGroups.TryGetValue(true, out var staticInstancingObjects))
                {
                    if (staticInstancingObjects.Count > 1)
                    {
                        Log(staticInstancing);
                        staticInstanced += staticInstancingObjects.Count;
                        staticInstancingCalls++;
                    }
                    else
                        staticBatchGroup.Add(staticInstancingObjects[0]);
                }
                if (instancingGroups.TryGetValue(false, out var dynamicInstancingObjects))
                {
                    if (dynamicInstancingObjects.Count > 1)
                    {
                        Log(dynamicInstancing);
                        dynamicInstanced += dynamicInstancingObjects.Count;
                        dynamicInstancingCalls++;
                    }
                    else
                        dynamicBatchGroup.Add(dynamicInstancingObjects[0]);
                }
            }
            Log(instancingEnd);

            Log(batchingBegin);

            if (staticBatchGroup.Count > 0)
            {
                Log(staticBatching);
                staticBatched += staticBatchGroup.Count;
                staticBatchingCalls++;
            }
            if (dynamicBatchGroup.Count > 0)
            {
                Log(dynamicBatching);
                dynamicBatched += dynamicBatchGroup.Count;
                dynamicBatchingCalls++;
            }
            Log(batchingEnd);
        }

        if (summaryLog)
        {
            Console.WriteLine($"shaderSwitches: {shaderSwitches}");
            Console.WriteLine($"materialSwitches: {materialSwitches}");

            Console.WriteLine($"drawObjects: {staticInstanced + dynamicInstanced + staticBatched + dynamicBatched}");
            Console.WriteLine($"drawCalls: {staticInstancingCalls + dynamicInstancingCalls + staticBatchingCalls + dynamicBatchingCalls}");

            Console.WriteLine($"staticInstancingCalls: {staticInstancingCalls}");
            Console.WriteLine($"dynamicInstancingCalls: {dynamicInstancingCalls}");

            Console.WriteLine($"staticBatchingCalls: {staticBatchingCalls}");
            Console.WriteLine($"dynamicBatchingCalls: {dynamicBatchingCalls}");

            Console.WriteLine($"staticInstanced: {staticInstanced}");
            Console.WriteLine($"dynamicInstanced: {dynamicInstanced}");

            Console.WriteLine($"staticBatched: {staticBatched}");
            Console.WriteLine($"dynamicBatched: {dynamicBatched}");
        }
    }


    private static void Log(string text)
    {
        if (log)
            Console.WriteLine(text);
    }
}
