using Arch.Core;
using Avalonia.Controls;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;
using ExCSS;
using System;
using System.Numerics;

namespace DeltaEditor;

internal partial class QuaternionNodeControl : InspectorNode
{
    private readonly NodeData _nodeData;
    public QuaternionNodeControl() => InitializeComponent();
    public QuaternionNodeControl(NodeData nodeData) : this()
    {
        FieldName.Content = (_nodeData = nodeData).FieldName;
        FieldX.OnDrag += x => _nodeData.DragFloat(FieldX.FieldData, x, 1);
        FieldY.OnDrag += x => _nodeData.DragFloat(FieldY.FieldData, x, 1);
        FieldZ.OnDrag += x => _nodeData.DragFloat(FieldZ.FieldData, x, 1);
    }

    public override void SetLabelColor(IBrush brush) => FieldName.Foreground = brush;

    public override bool UpdateData(ref EntityReference entity)
    {
        if (!ClipVisible)
            return false;

        var euler = Degrees(_nodeData.GetData<Quaternion>(ref entity));

        bool changed = SetField(FieldX.FieldData, ref euler.X) |
                       SetField(FieldY.FieldData, ref euler.Y) |
                       SetField(FieldZ.FieldData, ref euler.Z);
        if (changed)
            _nodeData.SetData(ref entity, ToQuaternion(euler));

        return changed;
    }

    private static bool SetField(TextBox field, ref float angle)
    {
        bool changed = field.IsFocused;
        if (!changed)
            field.Text = angle.ParseToString();
        else if (field.Text.ParseToFloat(out var parsed))
            angle = parsed;
        return changed;
    }


    public static Quaternion ToQuaternion(Vector3 v)
    {
        v = v / 360 * MathF.PI;
        (float sx, float cx) = MathF.SinCos(v.X);
        (float sy, float cy) = MathF.SinCos(v.Y);
        (float sz, float cz) = MathF.SinCos(v.Z);
        float cysz = cy * sz;
        float cycz = cy * cz;
        float sycz = sy * cz;
        float sysz = sy * sz;
        return new Quaternion
        {
            X = -(cx * sysz) + (sx * cycz),
            Y = (cx * sycz) + (sx * cysz),
            Z = (cx * cysz) - (sx * sycz),
            W = (cx * cycz) + (sx * sysz),
        };
    }

    public static Vector3 Degrees(Quaternion q)
    {
        var qY2 = q.Y * q.Y;
        float sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
        float siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
        float cosr_cosp = 1 - (2 * (q.X * q.X + qY2));
        float cosy_cosp = 1 - (2 * (qY2 + q.Z * q.Z));
        float sinp = 2 * (q.W * q.Y - q.Z * q.X);
        float toDegrees = 180f / MathF.PI;
        return new()
        {
            X = MathF.Atan2(sinr_cosp, cosr_cosp) * toDegrees,
            Y = (MathF.Abs(sinp) >= 1 ? MathF.CopySign(MathF.PI / 2, sinp) : MathF.Asin(sinp)) * toDegrees,
            Z = (MathF.Atan2(siny_cosp, cosy_cosp)) * toDegrees,
        };
    }

}