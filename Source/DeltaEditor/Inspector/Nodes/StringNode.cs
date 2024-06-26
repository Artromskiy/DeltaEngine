﻿using Arch.Core;
using DeltaEditor.Inspector.Internal;

namespace DeltaEditor.Inspector.Nodes;


internal class StringNode : FieldNode<string>
{
    public StringNode(NodeData parameters, bool withName = true) : base(parameters, withName)
    {

    }

    public override void UpdateData(EntityReference entity)
    {
        if (!_fieldData.IsFocused)
            _fieldData.Text = GetData(entity);
        else
            SetData(entity, _fieldData.Text);
    }

}