using Avalonia.Media;

namespace DeltaEditor.Tools
{
    internal class Colors
    {
        private static readonly Color PurpleX11 = new(255, 160, 32, 240);
        private static readonly Color PurpleMine = new(255, 175, 50, 240);
        public static readonly SolidColorBrush FocusedLabelBrush = new(PurpleMine);
        public static readonly SolidColorBrush UnfocusedLabelBrush = new(new Color(255, 255, 255, 255));
    }
}