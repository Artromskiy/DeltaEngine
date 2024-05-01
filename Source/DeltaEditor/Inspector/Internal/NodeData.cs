using Arch.Core;
using DeltaEditorLib.Loader;
using DeltaEditorLib.Scripting;
using System.Runtime.InteropServices;

namespace DeltaEditor.Inspector.Internal;

public class NodeData(RootData root, PathData path)
{
    public readonly RootData rootData = root;
    public readonly PathData pathData = path;

    public Type Component => rootData.Component;
    public string FieldName => Path.Length == 0 ? rootData.componentName : Path[^1];
    public IAccessorsContainer Accessors => rootData.Accessors;
    public Type FieldType => rootData.Accessors.GetFieldType(rootData.Component, Path);
    public ReadOnlySpan<string> Path => pathData.Path;
    public ReadOnlySpan<string> FieldNames => rootData.Accessors.AllAccessors[rootData.Accessors.GetFieldType(rootData.Component, Path)].FieldNames;
    public NodeData ChildData(string fieldName) => new(rootData, new([.. Path, fieldName]));
    public T GetData<T>(EntityReference entity) => rootData.Accessors.GetComponentFieldValue<T>(entity, rootData.Component, Path);
    public void SetData<T>(EntityReference entity, T data) => rootData.Accessors.SetComponentFieldValue(entity, rootData.Component, Path, data);
}
public record RootData(Type Component, RuntimeLoader RuntimeLoader)
{
    public readonly string componentName = Component.Name;
    public IAccessorsContainer Accessors = RuntimeLoader.Accessors;
}
public class PathData(List<string> path)
{
    private readonly List<string> _path = path;
    public ReadOnlySpan<string> Path => CollectionsMarshal.AsSpan(_path);
}
