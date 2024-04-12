using Delta.Files;
using Delta.Scripting;
using System.Numerics;

namespace DeltaEditor
{
    public class ComponentEditor : ContentView
    {

        private readonly HashSet<object> _processedObjects = [];

        public ComponentEditor(object component)
        {
            Content = GenerateEditor(component, component.GetType().Name);
        }

        private View GenerateEditor(object obj, string propertyName)
        {
            if (_processedObjects.Contains(obj))
            {
                return new ContentView();
            }

            _processedObjects.Add(obj);

            var stackLayout = new StackLayout();

            stackLayout.Children.Add(new Label { Text = propertyName });

            /*
            foreach (var property in obj.GetType().GetProperties())
                if (property.CanWrite && property.CanRead && (property.GetGetMethod()?.IsPublic == true || property.IsDefined(typeof(EditableAttribute))))
                    stackLayout.Children.Add(CreateEditorForProperty(property.Name, property.GetValue(obj)));
            */

            foreach (var field in obj.GetType().GetFields())
                if ((!field.IsStatic && field.IsPublic) || field.IsDefined(typeof(EditableAttribute), false))
                    stackLayout.Children.Add(CreateEditorForProperty(field.Name, field.GetValue(obj)));

            return stackLayout;
        }

        private static StackLayout CreateEditorForProperty(string propertyName, object value)
        {
            var stackLayout = new StackLayout();

            stackLayout.Children.Add(new Label { Text = propertyName });

            stackLayout.Children.Add(CreateView(value));

            return stackLayout;
        }

        private static View CreateView(object value)
        {
            return value switch
            {
                Vector2 vec2 => Vector2View(vec2),
                Vector3 vec3 => Vector3View(vec3),
                Quaternion quat => Quaternion3View(quat),
                Matrix4x4 mat4 => Matrix4x4View(mat4),
                IGuid => GuidAssetView(value),
                _ => DefaultView(value)
            };
        }

        static View DefaultView(object value)
        {
            if (value != null && !value.GetType().IsPrimitive && value.GetType() != typeof(string))
                return new ComponentEditor(value);
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