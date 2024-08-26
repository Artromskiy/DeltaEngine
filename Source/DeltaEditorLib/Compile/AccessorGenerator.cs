using Delta.Scripting;
using DeltaEditorLib.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;


namespace DeltaEditorLib.Compile;

internal class AccessorGenerator
{
    public static string GenerateAccessors(HashSet<Type> componentTypes)
    {
        HashSet<Type> visitedTypes = [];
        foreach (var item in componentTypes)
            if (item.IsPublic)
                GetAvaliableTypes(item, visitedTypes);
        var code = GenerateAccessorClasses(visitedTypes);
        code = CSharpSyntaxTree.ParseText(code).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
        return code;
    }

    public static void GetAvaliableTypes(Type type, HashSet<Type> visited)
    {
        if (visited.Contains(type))
            return;

        visited.Add(type);

        foreach (var field in SelectFields(type.GetFields()))
            AddField(field, visited);
    }

    private static void AddField(FieldInfo field, HashSet<Type> visited)
    {
        var type = field.FieldType;
        if (!type.IsPrimitive && type != typeof(string))
            GetAvaliableTypes(type, visited);
    }

    private static string GenerateAccessorClasses(HashSet<Type> types)
    {
        StringBuilder code = new();
        var fields = SelectFields(types.Select(t => t.GetFields()).SelectMany(f => f));
        var namespaces = fields.Select(f => f.FieldType.Namespace).Concat(types.Select(t => t.Namespace)).ToHashSet();
        GenerateUsings(code, namespaces!);
        code.Append($"public class AccessorsContainer: {nameof(IAccessorsContainer)}").AppendLine().
            Append('{').AppendLine();
        code.Append($"public FrozenDictionary<Type, {nameof(IAccessor)}> AllAccessors ").
            Append("{ get; }").Append($" = new Dictionary<Type, {nameof(IAccessor)}>()").AppendLine().
            Append('{').AppendLine();
        foreach (var item in types)
        {
            code.Append('{').Append($"typeof({GetFormattedName(item)}), new {GetAccessorName(item)}()").Append("},").
                AppendLine();
        }
        code.Append("}.ToFrozenDictionary();").AppendLine();

        foreach (var type in types)
            GenerateAccessorClass(code, type);
        code.AppendLine().
            Append('}');
        return code.ToString();
    }

    private static IEnumerable<FieldInfo> SelectFields(IEnumerable<FieldInfo> fields)
    {
        return fields.
            Where(f =>
            //!f.FieldType.IsGenericType &&
            f.FieldType.IsPublic &&
            !f.IsStatic &&
            (f.IsPublic || f.IsDefined(typeof(EditableAttribute), false)));
    }

    private static void GenerateAccessorClass(StringBuilder code, Type type)
    {
        var fields = SelectFields(type.GetFields());

        code.Append("private class ").Append(GetAccessorName(type)).Append($": {nameof(IAccessor)}");
        code.AppendLine();
        code.Append('{');
        code.AppendLine();

        GenerateFieldNamesGetter(code, fields);
        GenerateFieldValueGetter(code, fields, type);
        GenerateFieldTypeGetter(code, fields);
        GenerateFieldPointerGetter(code, fields, type);

        foreach (var field in fields)
        {
            GenerateFieldAccessor(code, field, type);
        }

        code.AppendLine().
        Append('}').
        AppendLine();
    }

    private static void GenerateFieldAccessor(StringBuilder sb, FieldInfo fieldInfo, Type container)
    {
        sb.Append("[UnsafeAccessor(UnsafeAccessorKind.Field, Name = \"").
            Append(fieldInfo.Name).
            Append("\")]").
            AppendLine();

        sb.Append("public extern static ref ").
            Append(GetFormattedName(fieldInfo.FieldType)).
            Append(' ').
            Append(GetSetMethodName(fieldInfo)).
            Append('(').
            Append("ref ").
            Append(GetFormattedName(container)).
            Append(" obj);").
            AppendLine();
    }

    public static void GenerateFieldTypeGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos)
    {
        sb.AppendLine();
        sb.Append("public Type GetFieldType(string name)");
        sb.AppendLine().Append('{').AppendLine();
        sb.Append("return name switch");
        sb.AppendLine().Append('{').AppendLine();
        foreach (var field in fieldInfos)
        {
            sb.Append('"').Append(field.Name).Append('"').Append($"=> typeof({GetFormattedName(field.FieldType)}),").AppendLine();
        }
        sb.Append("_ => throw new InvalidOperationException($\"Field with name {name} of type {typeof(Transform)} not found\")").AppendLine();
        sb.AppendLine().Append("};").AppendLine();
        sb.AppendLine().Append('}').AppendLine();
    }

    public static void GenerateFieldValueGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos, Type type)
    {
        sb.AppendLine();
        sb.Append("public object GetFieldValue(ref readonly object obj, string name)");
        sb.AppendLine().Append('{').AppendLine();
        sb.Append("switch (name)");
        sb.AppendLine().Append('{').AppendLine();
        foreach (var field in fieldInfos)
        {
            sb.Append($"case \"{field.Name}\":").AppendLine();
            sb.Append('{').AppendLine();
            sb.Append($"var val = ({GetFormattedName(type)})obj;").AppendLine();
            sb.Append($"return {GetSetMethodName(field)}(ref val);").AppendLine();
            sb.Append('}').AppendLine();
        }
        sb.Append("default: throw new InvalidOperationException($\"Field with name {name} of type {typeof(Transform)} not found\");").AppendLine();
        sb.AppendLine().Append("};").AppendLine();
        sb.AppendLine().Append('}').AppendLine();
    }

    private static void GenerateFieldNamesGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos)
    {
        sb.AppendLine();
        sb.Append("private readonly string[] _fieldNames = ").Append('[');
        foreach (var field in fieldInfos)
        {
            sb.Append($"\"{field.Name}\",").AppendLine();
        }
        sb.Append("];");
        sb.Append("public ReadOnlySpan<string> FieldNames => new(_fieldNames);").AppendLine();
    }

    private static void GenerateFieldPointerGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos, Type type)
    {
        sb.AppendLine();
        sb.Append("public unsafe nint GetFieldPtr(nint ptr, string name)");
        sb.AppendLine().Append('{').AppendLine();
        sb.Append($"ref var obj = ref Unsafe.AsRef<{GetFormattedName(type)}>(ptr.ToPointer());").AppendLine();
        sb.Append("return name switch");
        sb.AppendLine().Append('{').AppendLine();
        foreach (var field in fieldInfos)
        {
            sb.Append('"').Append(field.Name).Append('"').Append($"=> new nint(Unsafe.AsPointer(ref {GetSetMethodName(field)}(ref obj))),").AppendLine();
        }
        sb.Append("_ => throw new InvalidOperationException($\"Field with name {name} of type {typeof(Transform)} not found\")").AppendLine();
        sb.AppendLine().Append("};").AppendLine();
        sb.AppendLine().Append('}').AppendLine();
    }

    private static string GetSetMethodName(FieldInfo fieldInfo) => "GetSet_" + fieldInfo.Name;
    //private static string GetAccessorName(Type type) => type.Name + "Accessor";
    private static void GenerateUsings(StringBuilder sb, HashSet<string> namespaces)
    {
        namespaces.Add("System.Collections.Generic");
        namespaces.Add("System.Runtime.CompilerServices");
        namespaces.Add("System.Collections.Frozen");
        namespaces.Add("DeltaEditorLib.Scripting");

        foreach (var n in namespaces.Distinct())
            sb.Append("using ").Append(n).Append(';').AppendLine();
    }

    /// <summary>
    /// Returns the type name. If this is a generic type, appends
    /// the list of generic type arguments between angle brackets.
    /// (Does not account for embedded / inner generic arguments.)
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>System.String.</returns>
    private static string GetFormattedName(Type type)
    {
        if (type.IsGenericType)
        {
            string genericArguments = type.GetGenericArguments()
                                .Select(x => GetFormattedName(x))
                                .Aggregate((x1, x2) => $"{x1}, {x2}");
            const string g = "`";
            return $"{type.Name[..type.Name.IndexOf(g)]}<{genericArguments}>";
        }
        return type.Name;
    }
    private static string GetAccessorName(Type type)
    {
        return $"{GetAccessorNameArguments(type)}Accessor";
    }
    private static string GetAccessorNameArguments(Type type)
    {
        if (type.IsGenericType)
        {
            string genericArguments = type.GetGenericArguments()
                                .Select(x => GetFormattedName(x))
                                .Aggregate((x1, x2) => $"{x1}_{x2}");
            const string g = "`";
            return $"{type.Name[..type.Name.IndexOf(g)]}__{genericArguments}__";
        }
        return type.Name;
    }
}
