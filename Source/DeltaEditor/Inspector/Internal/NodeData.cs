using Arch.Core;
using Avalonia.Controls;
using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace DeltaEditor.Inspector.Internal;

public class NodeData(RootData root, PathData path)
{
    public readonly RootData rootData = root;
    public readonly PathData pathData = path;

    public NodeData(RootData root) : this(root, new([])) { }

    public Type Component => rootData.Component;
    public string FieldName => Path.Length == 0 ? rootData.componentName : Path[^1];
    public IAccessorsContainer Accessors => rootData.Accessors;
    public Type FieldType => rootData.Accessors.GetFieldType(rootData.Component, Path);
    public ReadOnlySpan<string> Path => pathData.Path;
    public ReadOnlySpan<string> FieldNames => rootData.Accessors.AllAccessors[rootData.Accessors.GetFieldType(rootData.Component, Path)].FieldNames;
    public NodeData ChildData(string fieldName) => new(rootData, new([.. Path, fieldName]));
    public T GetData<T>(ref EntityReference entity) => rootData.Accessors.GetComponentFieldValue<T>(entity, rootData.Component, Path);
    public void SetData<T>(ref EntityReference entity, T data) => rootData.Accessors.SetComponentFieldValue(entity, rootData.Component, Path, data);


    public bool UpdateFloat(TextBox fieldData, ref EntityReference entity)
    {
        bool changed = fieldData.IsFocused;
        if (!changed)
            fieldData.Text = GetData<float>(ref entity).ParseToString();
        else if (fieldData.Text.ParseToFloat(out var value))
            SetData(ref entity, value);
        return changed;
    }

    public void DragFloat(TextBox fieldData, float delta, float multiplier)
    {
        if (!fieldData.IsFocused)
            fieldData.Focus();

        if (fieldData.Text.ParseToFloat(out var value))
            value += delta * multiplier;
        fieldData.Text = value.ParseToStringHighRes();
    }

    public void DragInt(TextBox fieldData, float delta)
    {
        if (!fieldData.IsFocused)
            fieldData.Focus();
        if(int.TryParse(fieldData.Text, out int value))
            value += MathF.Sign(delta);
        fieldData.Text = value.ParseToString();
    }

    public bool UpdateString(TextBox FieldData, ref EntityReference entity)
    {
        bool changed = FieldData.IsFocused;
        if (!changed)
            FieldData.Text = GetData<string>(ref entity);
        else if (FieldData.Text != null)
            SetData(ref entity, FieldData.Text);
        return changed;
    }

    public bool UpdateInt(TextBox fieldData, ref EntityReference entity)
    {
        bool changed = fieldData.IsFocused;
        if (!changed)
            fieldData.Text = GetData<int>(ref entity).ParseToString();
        else
        {
            if (string.IsNullOrEmpty(fieldData.Text))
                SetData(ref entity, default(int));
            else if (int.TryParse(fieldData.Text, out int result))
                SetData(ref entity, result);
        }
        return changed;
    }
}

public record RootData(Type Component, IAccessorsContainer Accessors)
{
    public readonly string componentName = Component.Name;
    public IAccessorsContainer Accessors = Accessors;
}
public class PathData(List<string> path)
{
    private readonly List<string> _path = path;
    public ReadOnlySpan<string> Path => CollectionsMarshal.AsSpan(_path);
}
