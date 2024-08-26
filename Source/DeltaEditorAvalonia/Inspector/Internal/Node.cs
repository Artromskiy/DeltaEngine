using Arch.Core;
using System;

namespace DeltaEditorAvalonia.Inspector.Internal;

internal abstract class Node : INode
{
    protected readonly NodeData _nodeData;
    protected virtual bool SuppressTypeCheck => false;

    public Node(NodeData nodeData)
    {
        _nodeData = nodeData;
    }
    public abstract bool UpdateData(EntityReference entity);
}

internal abstract class Node<T> : Node
{
    public Node(NodeData nodeData) : base(nodeData)
    {
        if (!SuppressTypeCheck)
            ThrowHelper.CheckTypes(_nodeData);
    }

    public T GetData(EntityReference entity) => _nodeData.GetData<T>(entity);
    public void SetData(EntityReference entity, T value) => _nodeData.SetData(entity, value);


    private static class ThrowHelper
    {
        public static void CheckTypes(NodeData nodeData)
        {
            var type = nodeData.FieldType;
            if (type != typeof(T))
                throw new InvalidOperationException($"Type of field is not{nameof(T)} in path {string.Join(",", nodeData.Path.ToArray())}");
        }
    }
}
