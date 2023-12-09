using DeltaEngine.Collections;
using DeltaEngine.Files;
using DeltaEngine.Rendering;
using System;

namespace DeltaEngine.ECS;

public struct RenderData : IComparable<RenderData>
{
    public int id;
    public Transform transform;

    public MeshRenderer meshRenderer;

    public GuidAsset<MeshData> mesh;
    public GuidAsset<MaterialData> material;

    public int renderGroupId;
    public StackList<Transform> bindedGroup;
    public bool transformDirty;
    public bool isStatic;


    public readonly int CompareTo(RenderData other)
    {
        if (other.id == id)
            return 0;
        var shaderDiff = material.Asset.shader.guid.CompareTo(other.material.Asset.shader.guid);
        if (shaderDiff != 0)
            return shaderDiff;
        var matDiff = material.guid.CompareTo(other.material.guid);
        if (matDiff != 0)
            return matDiff;
        var staticDiff = isStatic.CompareTo(other.isStatic);
        if (staticDiff != 0)
            return staticDiff;
        return id.CompareTo(other.id);
    }
}
