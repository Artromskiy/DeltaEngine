using Delta.ECS.Components;
using Delta.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;

public delegate ref object FieldAccessor(ref object obj);
public delegate ref Return FieldAccessor<Return, Assing>(ref Assing obj);

public interface IAccessor
{
    public FieldAccessor<Return, Access> GetFieldAccessor<Return, Access>(string name);
    public Type GetFieldType(string name);
    public object GetFieldValue(ref object obj, string name);
    public ReadOnlySpan<string> FieldNames { get; }
}

public interface IAccessorsContainer
{
    public Dictionary<Type, IAccessor> AllAccessors { get; }
}

namespace DeltaEditorLib.Scripting
{
    internal class AccessorGenerator
    {
        public string GenerateAccessors(HashSet<Type> componentTypes)
        {
            HashSet<Type> visitedTypes = [];
            foreach (var item in componentTypes)
                if (item.IsPublic && !item.IsGenericType)
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
            var namespaces = fields.Select(f => f.FieldType.Namespace).Concat(types.Select(t => t.Namespace));
            GenerateUsings(code, namespaces);
            code.Append($"public class AccessorsContainer: {nameof(IAccessorsContainer)}").AppendLine().
                Append('{').AppendLine();
            code.Append($"public Dictionary<Type, {nameof(IAccessor)}> AllAccessors ").Append("{ get; } = new()").AppendLine().
                Append('{').AppendLine();
            foreach (var item in types)
            {
                code.Append('{').Append($"typeof({item.Name}), new {AccessorClassName(item)}()").Append("},").
                    AppendLine();
            }
            code.Append("};").AppendLine();

            GenerateCaster(code);

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
                !f.FieldType.IsGenericType &&
                f.FieldType.IsPublic &&
                !f.IsStatic &&
                (f.IsPublic || f.IsDefined(typeof(EditableAttribute), false)));
        }

        private static void GenerateAccessorClass(StringBuilder code, Type type)
        {
            var fields = SelectFields(type.GetFields());

            code.Append("private class ").Append(AccessorClassName(type)).Append($": {nameof(IAccessor)}");
            code.AppendLine();
            code.Append('{');
            code.AppendLine();

            //GenerateDictionary(code, fields, type);
            GenerateFieldNamesGetter(code, fields, type);
            GenerateAccessorGetter(code, fields, type);
            GenerateFieldValueGetter(code, fields, type);
            GenerateFieldTypeGetter(code, fields, type);

            foreach (var field in fields)
            {
                GenerateFieldAccessor(code, field);
            }

            code.AppendLine().
            Append('}').
            AppendLine();
        }

        private static void GenerateCaster(StringBuilder sb)
        {
            sb.Append("""
                            
                private static FieldAccessor<Return, Access> Caster<Return, Access, Return1, Access1>(FieldAccessor<Return1, Access1> accessorSource)
                {
                    return Invoke;
                    ref Return Invoke(ref Access a) => ref Unsafe.As<Return1, Return>(ref accessorSource(ref Unsafe.As<Access, Access1>(ref a)));
                }

                """);
        }

        private static void GenerateFieldAccessor(StringBuilder sb, FieldInfo fieldInfo)
        {
            sb.Append("[UnsafeAccessor(UnsafeAccessorKind.Field, Name = \"").
                Append(fieldInfo.Name).
                Append("\")]").
                AppendLine();

            sb.Append("public extern static ref ").
                Append(fieldInfo.FieldType.Name).
                Append(' ').
                Append(GetSetMethodName(fieldInfo)).
                Append('(').
                Append("ref ").
                Append(fieldInfo.ReflectedType.Name).
                Append(" obj);").
                AppendLine();
        }

        private static void GenerateAccessorGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos, Type accessor)
        {
            sb.Append("public FieldAccessor<Return, Access> GetFieldAccessor<Return, Access>(string name)");
            sb.AppendLine().Append('{').AppendLine();
            sb.Append("return name switch");
            sb.AppendLine().Append('{').AppendLine();
            foreach (var field in fieldInfos)
            {
                sb.Append('"').Append(field.Name).Append('"').Append("=> Caster<Return, Access, ").
                    Append($"{field.FieldType.Name}, {accessor.Name}>({GetSetMethodName(field)}),").AppendLine();
            }
            sb.Append("_=>throw new InvalidOperationException($\"Field {name} of type {typeof(Return)} not found in type {typeof(Access)}\")");
            sb.AppendLine().Append("};").AppendLine();
            sb.AppendLine().Append('}').AppendLine();
        }

        public static void GenerateFieldTypeGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos, Type accessor)
        {
            sb.AppendLine();
            sb.Append("public Type GetFieldType(string name)");
            sb.AppendLine().Append('{').AppendLine();
            sb.Append("return name switch");
            sb.AppendLine().Append('{').AppendLine();
            foreach (var field in fieldInfos)
            {
                sb.Append('"').Append(field.Name).Append('"').Append($"=> typeof({field.FieldType.Name}),").AppendLine();
            }
            sb.Append("_ => throw new InvalidOperationException($\"Field with name {name} of type {typeof(Transform)} not found\")").AppendLine();
            sb.AppendLine().Append("};").AppendLine();
            sb.AppendLine().Append('}').AppendLine();
        }

        public static void GenerateFieldValueGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos, Type accessor)
        {
            sb.AppendLine();
            sb.Append("public object GetFieldValue(ref object obj, string name)");
            sb.AppendLine().Append('{').AppendLine();
            sb.Append("switch (name)");
            sb.AppendLine().Append('{').AppendLine();
            foreach (var field in fieldInfos)
            {
                sb.Append($"case \"{field.Name}\":").AppendLine();
                sb.Append('{').AppendLine();
                sb.Append($"var val = ({accessor.Name})obj;").AppendLine();
                sb.Append($"return {GetSetMethodName(field)}(ref val);").AppendLine();
                sb.Append('}').AppendLine();
            }
            sb.Append("default: throw new InvalidOperationException($\"Field with name {name} of type {typeof(Transform)} not found\");").AppendLine();
            sb.AppendLine().Append("};").AppendLine();
            sb.AppendLine().Append('}').AppendLine();
        }

        private static void GenerateFieldNamesGetter(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos, Type accessor)
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
        private static string GetSetMethodName(FieldInfo fieldInfo) => "GetSet" + fieldInfo.Name;
        private static string AccessorClassName(Type type) => type.Name + "Accessor";
        private static void GenerateUsings(StringBuilder sb, IEnumerable<string> namespaces)
        {
            sb.Append("using System.Collections.Generic;").AppendLine();
            sb.Append("using System.Runtime.CompilerServices;").AppendLine();
            foreach (var n in namespaces.Distinct())
                sb.Append("using ").Append(n).Append(';').AppendLine();
        }

        public static ref Vector3 GetSetposition(ref Transform obj) => ref obj.position;
        public static ref object GetSetField(ref object obj) => ref Unsafe.NullRef<object>();
        public static Dictionary<string, FieldAccessor> FieldAccessors = new()
        {

                {"asdasd", new((ref object o) => ref Unsafe.As<Vector3, object>(ref GetSetposition(ref Unsafe.As<object, Transform>(ref o))))}

        };

        private static void Do()
        {
            FieldAccessor fa = GetSetField;
            FieldAccessor fa2 = new((ref object o) => ref Unsafe.As<Vector3, object>(ref GetSetposition(ref Unsafe.As<object, Transform>(ref o))));
        }
    }
}
