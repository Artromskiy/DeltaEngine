using Arch.Core;
using Avalonia.Interactivity;
using Avalonia.Media;
using Delta.Assets;
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

    private readonly IGuidAssetProxy _guidProxy;

    public GuidAssetNodeControl() => InitializeComponent();
    public GuidAssetNodeControl(NodeData nodeData) : this()
    {
        _nodeData = nodeData;
        _guidData = _nodeData.ChildData(_nodeData.FieldNames[0]);
        _assetType = _nodeData.FieldType.GenericTypeArguments[0];
        _guidProxy = (IGuidAssetProxy)Activator.CreateInstance(typeof(GuidAssetProxy<>).MakeGenericType(_assetType))!;
    }

    public override void SetLabelColor(IBrush brush) { }

    public override bool UpdateData(ref EntityReference entity, IRuntimeContext ctx)
    {
        if (!ClipVisible)
            return false;
        bool changed = _guidToSet.HasValue;
        if (changed)
        {
            _guidData.SetData(ref entity, _guidToSet!.Value);
            _guidToSet = null;
        }
        GuidLabel.Content = _guidProxy.GetName(_nodeData, ref entity, ctx);
        NameLabel.Content = _nodeData.FieldName;
        return changed;
    }


    private void OnSelectAssetClick(object? sender, RoutedEventArgs e)
    {
        if (Program.RuntimeLoader == null)
            return;
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


    private readonly struct GuidAssetProxy<T> : IGuidAssetProxy where T: class, IAsset
    {
        public string GetName(NodeData nodeData, ref EntityReference entityRef, IRuntimeContext ctx)=> nodeData.GetData<GuidAsset<T>>(ref entityRef).ToString();
    }

    private interface IGuidAssetProxy
    {
        public string GetName(NodeData nodeData, ref EntityReference entityRef, IRuntimeContext ctx);
    }
}