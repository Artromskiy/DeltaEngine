using Arch.Core;
using Avalonia.Controls;
using Avalonia.Media;
using DeltaEditor.Inspector.Internal;
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
    }

    public override void SetLabelColor(IBrush brush)=> FieldName.Foreground = brush;

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

    public static Vector3 Degrees(Quaternion r)
    {
        var rx2 = r.X * r.X;
        var x = MathF.Atan2(2.0f * ((r.Y * r.W) + (r.X * r.Z)), 1.0f - (2.0f * (rx2 + (r.Y * r.Y))));
        var z = MathF.Atan2(2.0f * ((r.X * r.Y) + (r.Z * r.W)), 1.0f - (2.0f * (rx2 + (r.Z * r.Z))));
        var y = MathF.Asin(2.0f * ((r.X * r.W) - (r.Y * r.Z)));
        return new Vector3(x, y, z) * 180 / MathF.PI;
    }

}