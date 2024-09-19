using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace DeltaEditor;

public sealed partial class AssetSearchNodeControl : UserControl, IDisposable
{
    private readonly AssetSearchControl _creator;
    public Guid assetGuid;
    public string GuidAssetName
    {
        set=> AssetName.Content = value;
    }

    public AssetSearchNodeControl() => InitializeComponent();
    public AssetSearchNodeControl(AssetSearchControl creator) : this()
    {
        _creator = creator;
    }

    public void Dispose()
    {
        assetGuid = Guid.Empty;
        _creator.ReturnNode(this);
    }
    private void AssetSelected(object? sender, TappedEventArgs e) => _creator?.SelectGuid(assetGuid);
}