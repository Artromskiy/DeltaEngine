using Delta.Files;
using Delta.Scripting;
using System.Numerics;

namespace DeltaEditor
{
    public class ComponentEditor : ContentView
    {
        //private readonly HashSet<object> _processedObjects = [];
        //private readonly IAccessorsContainer _accessors;
        private ComponentEditor(View view)
        {
            Content = view;
        }

        public static ComponentEditor? Create(object component, IAccessorsContainer accessors)
        {
            HashSet<object> visited = [];
            var editor  = GenerateEditor(component, component.GetType().Name, accessors, visited);
            if (editor != null)
                return new ComponentEditor(editor);
            return null;
        }

        private static View? GenerateEditor(object obj, string propertyName, IAccessorsContainer accessors, HashSet<object> visited)
        {
            if (visited.Contains(obj) || !obj.GetType().IsPublic)
            {
                return null;
            }

            visited.Add(obj);


            accessors.AllAccessors.TryGetValue(obj.GetType(), out var accessor);
            List<View> views = [];
            foreach (var fieldName in accessor.FieldNames)
            {
                var fieldData = accessor.GetFieldValue(ref obj, fieldName);
                var editorForProp = CreateEditorForProperty(fieldName, fieldData, accessors, visited);
                if (editorForProp != null)
                    views.Add(editorForProp);
            }
            StackLayout? stackLayout = null;
            if (views.Count != 0)
            {
                stackLayout = [new Label { Text = propertyName }];
                foreach (var view in views)
                    stackLayout.Children.Add(view);
            }
            /*
            foreach (var property in obj.GetType().GetProperties())
                if (property.CanWrite && property.CanRead && (property.GetGetMethod()?.IsPublic == true || property.IsDefined(typeof(EditableAttribute))))
                    stackLayout.Children.Add(CreateEditorForProperty(property.Name, property.GetValue(obj)));
            */
            /*
            foreach (var field in obj.GetType().GetFields())
                if ((!field.IsStatic && field.IsPublic) || field.IsDefined(typeof(EditableAttribute), false))
                    stackLayout.Children.Add(CreateEditorForProperty(field.Name, field.GetValue(obj)));
            */
            return stackLayout;
        }

        private static StackLayout? CreateEditorForProperty(string propertyName, object value, IAccessorsContainer accessors, HashSet<object> visited)
        {
            StackLayout? stackLayout = null;

            var view = CreateView(value, accessors, visited);
            if (view != null)
            {
                stackLayout = [new Label { Text = propertyName }];
                stackLayout.Children.Add(view);
            }

            return stackLayout;
        }

        private static View? CreateView(object value, IAccessorsContainer accessors, HashSet<object> visited)
        {
            return value switch
            {
                Vector2 vec2 => Vector2View(vec2),
                Vector3 vec3 => Vector3View(vec3),
                Quaternion quat => Quaternion3View(quat),
                Matrix4x4 mat4 => Matrix4x4View(mat4),
                IGuid => GuidAssetView(value),
                _ => DefaultView(value, accessors, visited)
            };
        }

        private static View? DefaultView(object value, IAccessorsContainer accessors, HashSet<object> visited)
        {
            if (value != null && !value.GetType().IsPrimitive && value.GetType() != typeof(string))
                return GenerateEditor(value, value.GetType().Name, accessors, visited);
            else
                return new Entry { Text = value?.ToString() };
        }


        private static HorizontalStackLayout Vector3View(Vector3 vector)
        {
            var stackLayout = new HorizontalStackLayout();
            stackLayout.Children.Add(new Entry { Text = vector.X.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.Y.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.Z.ToString() });
            return stackLayout;
        }
        private static HorizontalStackLayout Vector2View(Vector2 vector)
        {
            var stackLayout = new HorizontalStackLayout();
            stackLayout.Children.Add(new Entry { Text = vector.X.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.Y.ToString() });
            return stackLayout;
        }
        private static HorizontalStackLayout Vector4View(Vector4 vector)
        {
            var stackLayout = new HorizontalStackLayout();
            stackLayout.Children.Add(new Entry { Text = vector.X.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.Y.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.Z.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.W.ToString() });
            return stackLayout;
        }
        private static VerticalStackLayout Matrix4x4View(Matrix4x4 mat4)
        {
            var verticalStack = new VerticalStackLayout();
            verticalStack.Children.Add(Vector4View(new(mat4.M11, mat4.M12, mat4.M13, mat4.M14)));
            verticalStack.Children.Add(Vector4View(new(mat4.M21, mat4.M22, mat4.M23, mat4.M24)));
            verticalStack.Children.Add(Vector4View(new(mat4.M31, mat4.M32, mat4.M33, mat4.M34)));
            verticalStack.Children.Add(Vector4View(new(mat4.M41, mat4.M42, mat4.M43, mat4.M44)));
            return verticalStack;
        }

        private static HorizontalStackLayout Quaternion3View(Quaternion quaternion)
        {
            var stackLayout = new HorizontalStackLayout();
            var vector = Vector3.Transform(Vector3.UnitZ, quaternion);
            stackLayout.Children.Add(new Entry { Text = vector.X.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.Y.ToString() });
            stackLayout.Children.Add(new Entry { Text = vector.Z.ToString() });
            return stackLayout;
        }

        private static HorizontalStackLayout GuidAssetView(object guidAsset)
        {
            var t = guidAsset.GetType().GetGenericArguments()[0];
            var guid = (guidAsset as IGuid)!.GetGuid();
            var stackLayout = new HorizontalStackLayout();
            Span<byte> guidBytes = stackalloc byte[16];
            guid.TryWriteBytes(guidBytes);
            stackLayout.Children.Add(new Entry { Text = t.Name });
            stackLayout.Children.Add(new Entry { Text = Convert.ToBase64String(guidBytes) });
            return stackLayout;
        }
    }
}