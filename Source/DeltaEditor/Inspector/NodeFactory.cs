using Delta.Files;
using DeltaEditor.Inspector.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace DeltaEditor.Inspector;

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
            _ when type == typeof(Vector2) => new Vector2NodeControl(n),
            _ when type == typeof(Vector3) => new Vector3NodeControl(n),
            _ when type == typeof(Vector4) => new Vector4NodeControl(n),
            _ when type == typeof(Quaternion) => new QuaternionNodeControl(n),
            _ when type == typeof(Matrix4x4) => new Matrix4NodeControl(n),
            _ when type == typeof(float) => new FloatNodeControl(n),
            _ when type == typeof(int) => new IntNodeControl(n),
            _ when type == typeof(string) => new StringNodeControl(n),
            _ when IsGuidAssetType(type) => new GuidAssetNodeControl(n),
            _ => new CompositeNodeControl(n)
        };
    }

    private static bool IsGuidAssetType(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(GuidAsset<>);
}
