using Avalonia.Controls;
using Delta.Assets;
using Delta.Runtime;
using DeltaEditor.Hierarchy;
using System;
using System.Collections.Generic;

namespace DeltaEditor;

public partial class AssetSearchControl : UserControl
{
    private IGuidAssetProxy _genericProxy;
    private Action<Guid>? _onAssetSelected;

    public event Action<bool>? OnOpenedChanged;

    private readonly Stack<AssetSearchNodeControl> _cachedNodes = [];
    private IListWrapper<AssetSearchNodeControl, Control> ChildrenNodes => new(InspectorStack.Children);

    private static AssetSearchControl? _instance;
    public static AssetSearchControl Instance => _instance!;

    public AssetSearchControl()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;
        PanelHeader.OnCloseClick += CloseAssetSearch;
        _instance = this;
    }

    public void OpenAssetSearch(IRuntimeContext ctx, Type type, Action<Guid> onSelected)
    {
        IsVisible = true;
        OnOpenedChanged?.Invoke(true);
        _genericProxy = CreateProxy(type);
        _onAssetSelected = onSelected;
        var guids = _genericProxy.GetAssetsGuids(ctx);
        UpdateChildrenCount(guids.Length);
        for (int i = 0; i < guids.Length; i++)
        {
            ChildrenNodes[i].assetGuid = guids[i].guid;
            ChildrenNodes[i].GuidAssetName = guids[i].name;
        }
    }

    private void CloseAssetSearch()
    {
        _onAssetSelected = null;
        IsVisible = false;
        OnOpenedChanged?.Invoke(false);
    }

    public void SelectGuid(Guid guid)
    {
        _onAssetSelected?.Invoke(guid);
        CloseAssetSearch();
    }

    private void UpdateChildrenCount(int neededNodesCount)
    {
        var currentNodesCount = ChildrenNodes.Count;
        var delta = currentNodesCount - neededNodesCount;
        if (delta > 0)
        {
            for (int i = 0; i < delta; i++)
            {
                ChildrenNodes[^1].Dispose();
                ChildrenNodes.RemoveAt(ChildrenNodes.Count - 1);
            }
        }
        else if (delta < 0)
        {
            for (int i = delta; i < 0; i++)
            {
                var node = GetOrCreateNode();
                ChildrenNodes.Add(node);
            }
        }
    }

    public void ReturnNode(AssetSearchNodeControl node) => _cachedNodes.Push(node);
    private AssetSearchNodeControl GetOrCreateNode()
    {
        AssetSearchNodeControl node;
        if (!_cachedNodes.TryPop(out node!))
            node = new AssetSearchNodeControl(this);
        return node;
    }

    private IGuidAssetProxy CreateProxy(Type type)
    {
        var genericProxy = typeof(GenericGuidAssetProxy<>);
        return (IGuidAssetProxy)Activator.CreateInstance(genericProxy.MakeGenericType(type))!;
    }

    private readonly struct GenericGuidAssetProxy<T> : IGuidAssetProxy where T : class, IAsset
    {
        public readonly (Guid guid, string name)[] GetAssetsGuids(IRuntimeContext ctx)
        {
            var assets = ctx.AssetImporter.GetAllAssets<T>();
            int count = assets.Length;
            (Guid guid, string name)[] guids = new (Guid guid, string name)[count];
            for (int i = 0; i < count; i++)
                guids[i] = (assets[i].guid, assets[i].ToString());
            return guids;
        }
    }

    private interface IGuidAssetProxy
    {
        (Guid guid, string name)[] GetAssetsGuids(IRuntimeContext ctx);
    }
}