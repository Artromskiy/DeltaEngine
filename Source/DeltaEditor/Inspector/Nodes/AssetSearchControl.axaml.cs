using Avalonia.Controls;
using Avalonia.Interactivity;
using Delta.Files;
using Delta.Runtime;
using DeltaEditor.Hierarchy;
using System;
using System.Collections.Generic;

namespace DeltaEditor;

public partial class AssetSearchControl : UserControl
{
    private IGenericGuidAssetProxy _genericProxy;
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
            ChildrenNodes[i].AssetGuid = guids[i];
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
    private void OnCloseClick(object? sender, RoutedEventArgs e) => CloseAssetSearch();

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

    private IGenericGuidAssetProxy CreateProxy(Type type)
    {
        var genericProxy = typeof(GenericGuidAssetProxy<>);
        return (IGenericGuidAssetProxy)Activator.CreateInstance(genericProxy.MakeGenericType(type))!;
    }

    private readonly struct GenericGuidAssetProxy<T> : IGenericGuidAssetProxy where T : class, IAsset
    {
        public readonly Guid[] GetAssetsGuids(IRuntimeContext ctx)
        {
            var assets = ctx.AssetImporter.GetAllAssets<T>();
            int count = assets.Length;
            Guid[] guids = new Guid[count];
            for (int i = 0; i < count; i++)
                guids[i] = assets[i].guid;
            return guids;
        }
    }

    private interface IGenericGuidAssetProxy
    {
        Guid[] GetAssetsGuids(IRuntimeContext ctx);
    }
}