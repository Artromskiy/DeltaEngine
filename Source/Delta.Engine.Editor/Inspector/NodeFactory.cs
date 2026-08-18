using Delta.Engine.Assets;
using Delta.Engine.Editor.Inspector.Internal;
using Delta.Maths;
using System;
using System.Collections.Generic;

namespace Delta.Engine.Editor.Inspector;

internal static class NodeFactory
{
    private static readonly HashSet<Type> visited = [];
    public static InspectorNode CreateNode(NodeData nodeData)
    {
        var type = nodeData.FieldType;
        visited.Add(type);
        InspectorNode result = CreateNode(type, nodeData);
        visited.Remove(type);
        return result;
    }

    private static InspectorNode CreateNode(Type type, NodeData nodeData) => GetNode(type, nodeData);

    private static InspectorNode GetNode(Type type, NodeData n)
    {
        return type switch
        {
            _ when type == typeof(float2) => new Vector2NodeControl(n),
            _ when type == typeof(float3) => new Vector3NodeControl(n),
            _ when type == typeof(float4) => new Vector4NodeControl(n),
            _ when type == typeof(quaternion) => new QuaternionNodeControl(n),
            _ when type == typeof(float4x4) => new Matrix4NodeControl(n),
            _ when type == typeof(float) => new FloatNodeControl(n),
            _ when type == typeof(int) => new IntNodeControl(n),
            _ when type == typeof(string) => new StringNodeControl(n),
            _ when IsGuidAssetType(type) => new GuidAssetNodeControl(n),
            _ => new CompositeNodeControl(n)
        };
    }

    private static bool IsGuidAssetType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GuidAsset<>);
}
