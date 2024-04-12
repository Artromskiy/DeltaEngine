using Delta.ECS.Components;
using Delta.Scripting;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

public delegate ref object FieldAccessor(ref object obj);
public interface IAccessor
{
    public Dictionary<string, FieldAccessor> FieldAccessors { get; }
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
            return GenerateAccessorClasses(visitedTypes);
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

        public static string GenerateAccessorClasses(HashSet<Type> types)
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

        public static void GenerateAccessorClass(StringBuilder code, Type type)
        {
            var fields = SelectFields(type.GetFields());

            code.Append("private class ").Append(AccessorClassName(type)).Append($": {nameof(IAccessor)}");
            code.AppendLine();
            code.Append('{');
            code.AppendLine();

            GenerateDictionary(code, fields, type);

            foreach (var field in fields)
            {
                GenerateFieldAccessor(code, field);
            }

            code.AppendLine().
            Append('}').
            AppendLine();
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
                Append(" obj);");
        }


        private static void GenerateDictionary(StringBuilder sb, IEnumerable<FieldInfo> fieldInfos, Type accessor)
        {
            sb.Append($"public Dictionary<string, {nameof(FieldAccessor)}> {nameof(IAccessor.FieldAccessors)}").Append(" { get; } = new()").AppendLine().
                Append('{').AppendLine();
            foreach (var fieldInfo in fieldInfos)
            {
                sb.Append('{');
                sb.Append('"').Append(fieldInfo.Name).Append('"').Append(',').
                    Append($"new((ref object o) => ref Unsafe.As<{fieldInfo.FieldType.Name}, object>(ref {GetSetMethodName(fieldInfo)}(ref Unsafe.As<object, {accessor.Name}>(ref o))))").
                    Append("},").
                    AppendLine();
            }
            sb.Append("};").AppendLine();
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
