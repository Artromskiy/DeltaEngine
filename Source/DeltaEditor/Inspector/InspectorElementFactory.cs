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
            IInspectorElement result;
            if (type == typeof(Vector3))
                return new Vector3InspectorElement(parameters, visited, path);
            if (type.IsPrimitive || type == typeof(string))
                result = new EditorField(parameters, path);
            else
                result = new DefaultInspectorElement(parameters, visited, path);
            visited.Remove(type);
            if(result is StackLayout)
            {
                Console.WriteLine("wtf");
            }
            return result;
        }

        public static IInspectorElement CreateComponentInspector(InspectorElementParam parameters)
        {
            return new ComponentInspectorElement(parameters);
        }
    }
}
