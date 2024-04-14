using DeltaEditorLib.Scripting;
using System.Numerics;

namespace DeltaEditor.Inspector
{
    internal static class InspectorElementFactory
    {

        public static IInspectorElement CreateInspectorElement(InspectorElementParam parameters, HashSet<Type> visited, List<string> path)
        {
            var type = parameters.AccessorsContainer.GetFieldType(parameters.ComponentType, path);
            if (visited.Contains(type) || !type.IsPublic)
            {
                return new EmptyInspectorElement(path[^1]);
            }
            visited.Add(type);
            IInspectorElement result = CreateElement(type, parameters, visited, path);
            visited.Remove(type);
            return result;
        }

        private static IInspectorElement CreateElement(Type type, InspectorElementParam parameters, HashSet<Type> visited, List<string> path)
        {
            IInspectorElement result;
            if (type == typeof(Vector3))
                result = new Vector3InspectorElement(parameters, visited, path);
            else if (type == typeof(Vector4))
                result = new Vector4InspectorElement(parameters, visited, path);
            else if (type == typeof(Quaternion))
                result = new QuaternionInspectorElement(parameters, visited, path);
            else if (type == typeof(Matrix4x4))
                result = new Matrix4x4InspectorElement(parameters, visited, path);
            else if (type.IsPrimitive || type == typeof(string))
                result = new EditorField(parameters, path);
            else
                result = new DefaultInspectorElement(parameters, visited, path);
            return result;
        }

        public static IInspectorElement CreateComponentInspector(InspectorElementParam parameters)
        {
            return new ComponentInspectorElement(parameters);
        }
    }
}
