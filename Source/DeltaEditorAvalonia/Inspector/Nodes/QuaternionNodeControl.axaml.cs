using Arch.Core;
using Avalonia.Controls;
using DeltaEditorAvalonia.Inspector.Internal;
using System.Numerics;
using System;
using System.Threading.Channels;
using System.Xml.Linq;
using DeltaEditor.Tools;

namespace DeltaEditorAvalonia;

public partial class QuaternionNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    public QuaternionNodeControl() => InitializeComponent();
    public QuaternionNodeControl(NodeData nodeData) : this()
    {
        _nodeData = nodeData;
    }

    public bool UpdateData(EntityReference entity)
    {
        var quatRotation = _nodeData.GetData<Quaternion>(entity);
        var euler = Degrees(quatRotation);
        bool changed = SetField(FieldDataX, ref euler.X) |
                       SetField(FieldDataY, ref euler.Y) |
                       SetField(FieldDataZ, ref euler.Z);
        quatRotation = ToQuaternion(euler);
        _nodeData.SetData(entity, quatRotation);
        return changed;
    }

    private static bool SetField(TextBox field, ref float angle)
    {
        bool changed = field.IsFocused;
        if (!changed)
            field.Text = angle.LookupString();
        else
        {
            if (string.IsNullOrEmpty(field.Text))
                angle = default;
            else if (float.TryParse(field.Text, out var parsed))
                angle = parsed;
        }
        return changed;
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