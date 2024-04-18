using Arch.Core;
using DeltaEditorLib.Scripting;
using System.Runtime.InteropServices;

namespace DeltaEditor.Inspector
{
    public class NodeData(Type component, IAccessorsContainer accessors, List<string> path)
    {
        private readonly Type _component = component;
        private readonly List<string> _path = path;
        private readonly IAccessorsContainer _accessors = accessors;
        private readonly string _componentName = component.Name;

        public Type Component => _component;
        public string FieldName => Path.Length == 0? _componentName : Path[^1];
        public IAccessorsContainer Accessors => _accessors;
        public Type FieldType => _accessors.GetFieldType(_component, Path);
        public ReadOnlySpan<string> Path => CollectionsMarshal.AsSpan(_path);
        public ReadOnlySpan<string> FieldNames => _accessors.AllAccessors[_accessors.GetFieldType(_component, Path)].FieldNames;
        public NodeData ChildData(string fieldName) => new(_component, _accessors, [.. Path, fieldName]);
        public T GetData<T>(EntityReference entity) => _accessors.GetComponentFieldValue<T>(entity, _component, Path);
        public void SetData<T>(EntityReference entity, T data) => _accessors.SetComponentFieldValue(entity, _component, Path, data);
    }
}
