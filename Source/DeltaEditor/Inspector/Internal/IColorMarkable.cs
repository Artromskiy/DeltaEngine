using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;

namespace DeltaEditor.Inspector.Internal
{
    internal interface IColorMarkable
    {
        //public Control MarkRoot { get; }
        public void SetLabelColor(IBrush brush);
        public void SetBorderColor(IBrush brush);

        private static IColorMarkable? _markedNode;
        public static IColorMarkable? MarkedNode
        {
            get => _markedNode;
            set
            {
                if (MarkedNode != null)
                {
                    var labelBrush = Tools.Colors.DefaultLabelBrush;
                    var borderBrush = Tools.Colors.DefaultBorderBrush;
                    foreach (var item in (MarkedNode as Control)!.GetSelfAndVisualAncestors())
                        if (item is InspectorNode node)
                        {
                            node.SetLabelColor(labelBrush);
                            node.SetBorderColor(borderBrush);
                        }
                }
                _markedNode = value;
                if (MarkedNode != null)
                {
                    var brush = Tools.Colors.DefaultBorderFocusBrush;
                    foreach (var item in (MarkedNode as Control)!.GetSelfAndVisualAncestors())
                        if (item is InspectorNode node)
                        {
                            node.SetLabelColor(brush);
                            node.SetBorderColor(brush);
                        }
                }
            }
        }

        public static void TryMark(Control control)
        {
            foreach (var item in control.GetSelfAndVisualAncestors())
                if (item is IColorMarkable node)
                {
                    MarkedNode = node;
                    break;
                }
        }

        public static void Unmark()
        {
            MarkedNode = null;
        }
    }
}
