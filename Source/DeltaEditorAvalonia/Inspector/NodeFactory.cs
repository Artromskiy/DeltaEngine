using DeltaEditorAvalonia.Inspector.Internal;
using System;
using System.Collections.Generic;
using System.Numerics;
namespace DeltaEditorAvalonia.Inspector;

internal static class NodeFactory
{
    private static readonly HashSet<Type> visited = [];
    public static INode CreateNode(NodeData nodeData)
    {
        var type = nodeData.FieldType;
        //if (visited.Contains(type) || !type.IsPublic)
        //    return new EmptyNode(nodeData);
        visited.Add(type);
        INode result = CreateNode(type, nodeData);
        visited.Remove(type);
        return result;
    }

    private static INode CreateNode(Type type, NodeData nodeData)
    {
        if (!_typeToNode.TryGetValue(type, out var createFunc))
            return new CompositeNodeControl(nodeData);
        return createFunc(nodeData);
    }


    private static readonly Dictionary<Type, Func<NodeData, INode>> _typeToNode = new()
    {
        { typeof(Vector3), (n) => new Vector3NodeControl(n) },
        { typeof(Vector4), (n) => new Vector4NodeControl(n) },
        { typeof(Quaternion), (n) => new QuaternionNodeControl(n) },
        //{ typeof(Matrix4x4), (n) => new Matrix4Node(n) },
        { typeof(float), (n) => new FloatNodeControl(n) },
        { typeof(int), (n) => new IntNodeControl(n) },
        { typeof(string), (n) => new StringNodeControl(n) },
        //{ typeof(Guid), (n) => new GuidNode(n) },
    };

    public static INode CreateComponentInspector(NodeData parameters) => new ComponentNodeControl(parameters);
}
