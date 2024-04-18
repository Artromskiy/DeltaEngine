using DeltaEditor.Inspector.InspectorElements;
using DeltaEditor.Inspector.InspectorFields;
using System.Numerics;

namespace DeltaEditor.Inspector;

internal static class NodeFactory
{
    private static readonly HashSet<Type> visited = [];
    public static INode CreateNode(NodeData nodeData)
    {
        var type = nodeData.FieldType;
        if (visited.Contains(type) || !type.IsPublic)
            return new EmptyNode(nodeData);
        visited.Add(type);
        INode result = CreateNode(type, nodeData);
        visited.Remove(type);
        return result;
    }

    private static INode CreateNode(Type type, NodeData nodeData)
    {
        if (!_typeToNode.TryGetValue(type, out var createFunc))
            return new ContainerNode(nodeData);
        return createFunc(nodeData);
    }

    private static INode CreateNodeOld(Type type, NodeData nodeData)
    {
        INode result;
        if (type == typeof(Vector3))
            result = new Vector3Node(nodeData);
        else if (type == typeof(Vector4))
            result = new Vector4Node(nodeData);
        else if (type == typeof(Quaternion))
            result = new QuaternionNode(nodeData);
        else if (type == typeof(Matrix4x4))
            result = new Matrix4Node(nodeData);
        else if (type == typeof(float))
            result = new FloatNode(nodeData);
        else if (type == typeof(int))
            result = new IntNode(nodeData);
        else if (type == typeof(string))
            result = new StringNode(nodeData);
        else
            result = new ContainerNode(nodeData);
        return result;
    }

    private static readonly Dictionary<Type, Func<NodeData, INode>> _typeToNode = new()
    {
        { typeof(Vector3), (n) => new Vector3Node(n) },
        { typeof(Vector4), (n) => new Vector4Node(n) },
        { typeof(Quaternion), (n) => new QuaternionNode(n) },
        { typeof(Matrix4x4), (n) => new Matrix4Node(n) },
        { typeof(float), (n) => new FloatNode(n) },
        { typeof(int), (n) => new IntNode(n) },
        { typeof(string), (n) => new StringNode(n) },
    };

    public static INode CreateComponentInspector(NodeData parameters) => new ComponentNode(parameters);
}
