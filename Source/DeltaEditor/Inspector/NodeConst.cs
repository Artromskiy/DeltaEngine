using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DeltaEditor.Inspector.Node;

namespace DeltaEditor.Inspector;

internal static class NodeConst
{
    public const double NodeHeight = 30;

    public static double SizeModeToSize(FieldSizeMode sizeMode)
    {
        return sizeMode switch
        {
            FieldSizeMode.Default => 80,
            FieldSizeMode.Large => 120,
            FieldSizeMode.Small => 40,
            FieldSizeMode.ExtraSmall => 30,
            _ => throw new NotImplementedException(),
        };
    }
}

internal enum FieldSizeMode
{
    Default,
    Large,
    Small,
    ExtraSmall
}