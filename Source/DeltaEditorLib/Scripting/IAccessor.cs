using System;
using System.Collections.Frozen;

namespace DeltaEditorLib.Scripting;

public interface IAccessor
{
    public Type GetFieldType(string name);
    public object GetFieldValue(ref readonly object obj, string name);
    public nint GetFieldPtr(nint ptr, string name);
    public ReadOnlySpan<string> FieldNames { get; }
}

public interface IAccessorsContainer
{
    public FrozenDictionary<Type, IAccessor> AllAccessors { get; }
}