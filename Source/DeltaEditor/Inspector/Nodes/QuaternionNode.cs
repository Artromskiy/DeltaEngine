using Arch.Core;
using DeltaEditor.Inspector.Internal;
using System.Numerics;

namespace DeltaEditor.Inspector.Nodes;


internal class QuaternionNode : Node<Quaternion>
{
    private readonly HorizontalStackLayout _stack;

    private readonly string[] names = ["X", "Y", "Z"];
    private readonly List<DummyNode<float>> _inspectorElements;

    public QuaternionNode(NodeData parameters) : base(parameters)
    {
        _stack = [_fieldName];
        _stack.BackgroundColor = NodeConst.BackColor;
        _inspectorElements = [];
        foreach (var item in names)
        {
            var element = new DummyNode<float>(_nodeData.ChildData(item)) { NameMode = FieldSizeMode.ExtraSmall };
            _inspectorElements.Add(element);
            _stack.Add(element);
        }
        Content = _stack;
    }

    public override void UpdateData(EntityReference entity)
    {
        var quatRotation = GetData(entity);
        var euler = Degrees(quatRotation);
        for (int i = 0; i < _inspectorElements.Count; i++)
        {
            var element = _inspectorElements[i];
            if (element.FocusedField && string.IsNullOrEmpty(element.Value))
                euler[i] = default;
            else if (element.FocusedField && float.TryParse(element.Value, out var value))
                euler[i] = value;
            else if (!element.FocusedField)
                element.Value = euler[i].ToString("0.00");
        }
        quatRotation = ToQuaternion(euler);
        SetData(entity, quatRotation);
    }

    public static Quaternion ToQuaternion(Vector3 v)
    {
        v = v / 360 * MathF.PI;
        (float sy, float cy) = MathF.SinCos(v.Z);
        (float sp, float cp) = MathF.SinCos(v.Y);
        (float sr, float cr) = MathF.SinCos(v.X);

        return new Quaternion
        {
            W = (cr * cp * cy) + (sr * sp * sy),
            X = (sr * cp * cy) - (cr * sp * sy),
            Y = (cr * sp * cy) + (sr * cp * sy),
            Z = (cr * cp * sy) - (sr * sp * cy)
        };

    }
    public static Vector3 Degrees(Quaternion r)
    {
        var x = MathF.Atan2(2.0f * ((r.Y * r.W) + (r.X * r.Z)), 1.0f - (2.0f * ((r.X * r.X) + (r.Y * r.Y))));
        var y = MathF.Asin(2.0f * ((r.X * r.W) - (r.Y * r.Z)));
        var z = MathF.Atan2(2.0f * ((r.X * r.Y) + (r.Z * r.W)), 1.0f - (2.0f * ((r.X * r.X) + (r.Z * r.Z))));
        return new Vector3(x, y, z) * 180 / MathF.PI;
    }
}
