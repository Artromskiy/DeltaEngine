namespace DeltaEditor.Inspector.Internal;

internal static class NodeConst
{
    public const double ComponentHeaderTextSize = 15f;
    public const double NodeHeight = 30;
    public static readonly Color BackColor = Color.FromRgba(0, 0, 0, 0);
    public static readonly Color BorderColor = Color.FromRgb(15, 15, 15);
}

internal enum FieldSizeMode
{
    Default,
    Small,
    ExtraSmall,
    Large,
    ExtraLarge,
}