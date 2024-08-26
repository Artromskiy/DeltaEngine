namespace DeltaEditor.Inspector.Internal;

internal static class NodeConst
{
    public const double ComponentHeaderTextSize = 15f;
    public const double NodeHeight = 30;
    public const double Spacing = 10;
    public static readonly Color BackColor = Color.FromRgba(0, 0, 0, 0);
    public static readonly Color BorderColor = Color.FromRgb(15, 15, 15);

    public static readonly Color SelectedColor = Color.FromRgb(20, 5, 30);
    public static readonly Color NotSelectedColor = Color.FromRgba(0, 0, 0, 0);
}

internal enum FieldSizeMode
{
    Default,
    Small,
    ExtraSmall,
    Large,
    ExtraLarge,
}