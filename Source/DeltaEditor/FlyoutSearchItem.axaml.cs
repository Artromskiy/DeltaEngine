using Avalonia.Controls;
using Avalonia.Input;

namespace DeltaEditor;

public partial class FlyoutSearchItem : UserControl
{
    private readonly FlyoutSearchControl _creator;
    private bool _selected;
    private ISearchFlyoutViewModel _vm;
    public ISearchFlyoutViewModel VM
    {
        get => _vm;
        set
        {
            _vm = value;
            AssetNameLabel.Content = value.GetName;
        }
    }

    public FlyoutSearchItem() => InitializeComponent();
    public FlyoutSearchItem(FlyoutSearchControl creator) : this()
    {
        _creator = creator;
    }


    public bool Selected
    {
        get => _selected;
        set
        {
            _selected = value;
            BorderBrush = value ? Tools.Colors.DefaultBorderFocusBrush : Tools.Colors.DefaultBorderBrush;
        }
    }

    private void OnTap(object? sender, TappedEventArgs e) => _creator?.SelectItem(_vm);

    private void UserControl_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (!_selected)
            BorderBrush = Tools.Colors.DefaultBorderFocusBrush;
    }

    private void UserControl_PointerExited(object? sender, PointerEventArgs e)
    {
        if (!_selected)
            BorderBrush = Tools.Colors.DefaultBorderBrush;
    }
}