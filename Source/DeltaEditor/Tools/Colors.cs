using Avalonia.Media;

namespace DeltaEditor.Tools
{
    internal class Colors
    {
        private static readonly Color PurpleMine = new(255, 175, 50, 240);
        private static readonly Color Gray = new(30, 255, 255, 255);
        private static readonly Color Transparent = new(0, 255, 255, 255);
        public static readonly SolidColorBrush TransparentBrush = new(Transparent);
        public static readonly SolidColorBrush FocusedBrush = new(PurpleMine);
        public static readonly SolidColorBrush UnfocusedBorderBrush = new(Gray);
        public static readonly SolidColorBrush UnfocusedLabelBrush = new(new Color(255, 255, 255, 255));
    }
}