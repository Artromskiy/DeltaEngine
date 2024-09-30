using Arch.Core;
using Avalonia.Input;
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
        _guidProxy = CreateProxy(_assetType);
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
        GuidLabel.Content = _guidProxy.GetName(ref entity, _nodeData);
        NameLabel.Content = _nodeData.FieldName;
        return changed;
    }


    private void OnSelectAssetClick(object? sender, RoutedEventArgs e)
    {
        IColorMarkable.MarkedNode = this;

        FlyoutSearchControl.Instance.OpenAssetSearch(GuidLabel, _guidProxy.GetSearchVMs(),
            guid => _guidToSet = ((SearchFlyoutViewModel<Guid>)guid).Data);
    }


    private static IGuidAssetProxy CreateProxy(Type type)
    {
        var genericProxy = typeof(GenericGuidAssetProxy<>);
        return (IGuidAssetProxy)Activator.CreateInstance(genericProxy.MakeGenericType(type))!;
    }

    private readonly struct GenericGuidAssetProxy<T> : IGuidAssetProxy where T : class, IAsset
    {
        public readonly ISearchFlyoutViewModel[] GetSearchVMs()
        {
            var assets = IRuntimeContext.Current.AssetImporter.GetAllAssets<T>();
            int count = assets.Length;
            ISearchFlyoutViewModel[] guids = new ISearchFlyoutViewModel[count];
            for (int i = 0; i < count; i++)
                guids[i] = new SearchFlyoutViewModel<Guid>(assets[i].guid, assets[i].GetAssetNameOrDefault());
            return guids;
        }
        public readonly string GetName(ref EntityReference entityRef, NodeData nodeData)
        {
            var data = nodeData.GetData<GuidAsset<T>>(ref entityRef);
            return data.GetAssetNameOrDefault();
        }
    }

    private interface IGuidAssetProxy
    {
        ISearchFlyoutViewModel[] GetSearchVMs();
        public string GetName(ref EntityReference entityRef, NodeData nodeData);
    }

    private void UserControl_PointerEntered(object? sender, PointerEventArgs e)
    {
        if (IColorMarkable.MarkedNode == this)
            return;
        GuidLabel.BorderBrush = Tools.Colors.DefaultBorderOverBrush;
        SelectAssetButton.BorderBrush = Tools.Colors.DefaultBorderOverBrush;
    }

    private void UserControl_PointerExited(object? sender, PointerEventArgs e)
    {
        if (IColorMarkable.MarkedNode == this)
            return;
        GuidLabel.BorderBrush = Tools.Colors.DefaultBorderBrush;
        SelectAssetButton.BorderBrush = Tools.Colors.DefaultBorderBrush;
    }

    public override void SetLabelColor(IBrush brush)
    {
        NameLabel.Foreground = brush;
    }
    public override void SetBorderColor(IBrush brush)
    {
        GuidLabel.BorderBrush = brush;
        SelectAssetButton.BorderBrush = brush;
    }
}