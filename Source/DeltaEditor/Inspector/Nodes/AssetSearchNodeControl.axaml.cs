using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace DeltaEditor;

public sealed partial class AssetSearchNodeControl : UserControl, IDisposable
{
    private readonly AssetSearchControl _creator;
    private Guid _assetGuid;
    public Guid AssetGuid
    {
        get => _assetGuid;
        set => AssetName.Content = (_assetGuid = value).ToString();
    }

    public AssetSearchNodeControl() => InitializeComponent();
    public AssetSearchNodeControl(AssetSearchControl creator) : this()
    {
        _creator = creator;
    }

    public void Dispose()
    {
        AssetGuid = Guid.Empty;
        _creator.ReturnNode(this);
    }
    private void AssetSelected(object? sender, TappedEventArgs e) => _creator?.SelectGuid(_assetGuid);
}