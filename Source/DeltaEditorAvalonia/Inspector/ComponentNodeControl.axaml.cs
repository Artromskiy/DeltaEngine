using Avalonia;
using Avalonia.Controls;

namespace DeltaEditorAvalonia;

public partial class ComponentNodeControl : UserControl
{
    public static readonly AvaloniaProperty<Control?> ComponentContentProperty =
        AvaloniaProperty.Register<ComponentNodeControl, Control?>(nameof(ComponentContent));

    public Control? ComponentContent
    {
        get => BorderContent.Child;
        set => BorderContent.Child = value;
    }

    public ComponentNodeControl()
    {
        InitializeComponent();
    }
}