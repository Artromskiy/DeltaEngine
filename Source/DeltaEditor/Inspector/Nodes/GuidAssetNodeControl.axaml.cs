using Arch.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;
using System;

namespace DeltaEditor;

public partial class GuidAssetNodeControl : UserControl, INode
{
    private readonly NodeData _nodeData;
    private readonly NodeData _guidData;
    private readonly Type _assetType;

    private Guid? _guidToSet;

    public GuidAssetNodeControl() => InitializeComponent();
    public GuidAssetNodeControl(NodeData nodeData) : this()
    {
        _nodeData = nodeData;
        _guidData = _nodeData.ChildData(_nodeData.FieldNames[0]);
        _assetType = _nodeData.FieldType.GenericTypeArguments[0];
    }

    public bool UpdateData(ref EntityReference entity)
    {
        bool changed = _guidToSet != null;
        if (changed)
        {
            _guidData.SetData(ref entity, _guidToSet.Value);
            _guidToSet = null;
        }
        var currentGuid = _guidData.GetData<Guid>(ref entity);
        GuidLabel.Content = GuidToShortString(currentGuid);
        NameLabel.Content = _nodeData.FieldName;
        return changed;
    }

    private string GuidToShortString(Guid guid)
    {
        Span<byte> guidBytes = stackalloc byte[16];
        guid.TryWriteBytes(guidBytes);
        return Convert.ToBase64String(guidBytes);
    }

    private void OnSelectAssetClick(object? sender, RoutedEventArgs e)
    {
        Program.RuntimeLoader.OnUIThread += OpenAssetSearch;
        void OpenAssetSearch(IRuntimeContext ctx)
        {
            AssetSearchControl.Instance.OpenAssetSearch(ctx, _assetType, guid => _guidToSet = guid);
        }
    }
}