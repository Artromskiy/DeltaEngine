using Avalonia.Media;

namespace DeltaEditor.Tools
{
    internal class Colors
    {
        private static readonly Color Over = new(255, 193, 96, 246);
        private static readonly Color Focus = new(255, 175, 50, 240);
        private static readonly Color Default = new(30, 255, 255, 255);
        private static readonly Color Transparent = new(0, 255, 255, 255);
        public static readonly SolidColorBrush TransparentBrush = new(Transparent);
        public static readonly SolidColorBrush DefaultBorderFocusBrush = new(Focus);
        public static readonly SolidColorBrush DefaultBorderOverBrush = new(Over);
        public static readonly SolidColorBrush DefaultBorderBrush = new(Default);
        public static readonly SolidColorBrush DefaultLabelBrush = new(new Color(255, 255, 255, 255));
    }
}