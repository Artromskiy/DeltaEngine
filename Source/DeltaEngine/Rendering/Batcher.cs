using DeltaEngine.Files;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace DeltaEngine.Rendering;

public static class Batcher
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

    public static RenderData IterateWithCopy(RenderData[] data)
    {
        var vectorSize = Unsafe.SizeOf<Vector3>(); // 12
        var quatSize = Unsafe.SizeOf<Quaternion>(); // 16
        var guiadSize = Unsafe.SizeOf<Guid>(); // 16
        var transformSize = Unsafe.SizeOf<Transform>(); // 40
        var guiadAssetSize = Unsafe.SizeOf<GuidAsset<MeshData>>(); // 24
        var booleanSize = Unsafe.SizeOf<bool>(); // 1
        var renderDataSize = Unsafe.SizeOf<RenderData>(); // 96
        RenderData def = default;
        ref RenderData copyTo = ref def;
        for (int i = 0; i < data.Length; i++)
            copyTo = ref data[i];
        return copyTo;
    }

    /// <summary>
    /// Draft drawing method
    /// </summary>
    /// <param name="data"></param>
    public static void Draw2(RenderData[] data, bool summaryLog, bool fullLog = false)
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
