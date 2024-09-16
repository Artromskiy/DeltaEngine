using Arch.Core;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;
using System;

namespace DeltaEditor;

internal partial class GuidAssetNodeControl : InspectorNode
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

    public override void SetLabelColor(IBrush brush)
    {
        //throw new NotImplementedException();
    }

    public override bool UpdateData(ref EntityReference entity)
    {
        if (!ClipVisible)
            return false;
        bool changed = _guidToSet.HasValue;
        if (changed)
        {
            _guidData.SetData(ref entity, _guidToSet!.Value);
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
            AssetSearchControl.Instance.OpenAssetSearch(ctx, _assetType, guid =>
            {
                _guidToSet = guid;
                // in case we somehow scrolled out and field will not be updated as it's out of render viewport
                Focus();
            });
        }
    }
}