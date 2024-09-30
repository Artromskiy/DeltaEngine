using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DeltaEditor.Hierarchy;
using DeltaEditor.Inspector.Internal;
using System;
using System.Collections.Generic;

namespace DeltaEditor;

public partial class FlyoutSearchControl : UserControl
{
    private Action<ISearchFlyoutViewModel>? _onSelected;

    private string? _searchString;
    private int _selectedNodeIndex;

    private readonly List<ISearchFlyoutViewModel> _vms = [];
    private readonly Stack<FlyoutSearchItem> _cachedNodes = [];
    private readonly Flyout _parentFlyout;
    private IListWrapper<FlyoutSearchItem, Control> ChildrenNodes => new(ChildrenStackPanel.Children);

    private static FlyoutSearchControl? _instance;
    public static FlyoutSearchControl Instance => _instance!;

    public FlyoutSearchControl()
    {
        InitializeComponent();
        if (Design.IsDesignMode)
            return;
        _parentFlyout = new();
        _parentFlyout.Content = this;
        _instance = this;
    }

    public void OpenAssetSearch(Control caller, ISearchFlyoutViewModel[] vms, Action<ISearchFlyoutViewModel> onSelected)
    {
        IColorMarkable.TryMark(caller);
        _onSelected = onSelected;
        _vms.Clear();
        _vms.AddRange(vms);
        _parentFlyout.ShowAt(caller);
        SearchTextBox.Focus();
        UpdateNodes();
    }

    private void CloseAssetSearch()
    {
        _onSelected = null;
        SearchTextBox.Text = string.Empty;
        _parentFlyout.Hide();
    }

    public void SelectItem(ISearchFlyoutViewModel vm)
    {
        _onSelected?.Invoke(vm);
        CloseAssetSearch();
    }

    private void UpdateNodes()
    {
        _selectedNodeIndex = 0;
        foreach (var item in ChildrenNodes)
            _cachedNodes.Push(item);
        ChildrenNodes.Clear();

        foreach (var vm in _vms)
            if (string.IsNullOrEmpty(_searchString) || vm.GetName.Contains(_searchString, StringComparison.InvariantCultureIgnoreCase))
                ChildrenNodes.Add(GetOrCreateNode(vm));
        if (ChildrenNodes.Count != 0)
            ChildrenNodes[_selectedNodeIndex].Selected = true;
    }


    private void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ChildrenNodes.Count != 0)
        {
            SearchTextBox.Text = string.Empty;
            SelectItem(ChildrenNodes[_selectedNodeIndex].VM);
        }
        else if (e.Key == Key.Down)
        {
            _selectedNodeIndex = int.Min(_selectedNodeIndex + 1, ChildrenNodes.Count - 1);
            UpdateSelection();
        }
        else if (e.Key == Key.Up)
        {
            _selectedNodeIndex = int.Max(0, _selectedNodeIndex - 1);
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        foreach (var item in ChildrenNodes)
            item.Selected = false;
        if (ChildrenNodes.Count != 0)
            ChildrenNodes[_selectedNodeIndex].Selected = true;
    }

    private void SearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchString = SearchTextBox.Text;
        UpdateNodes();
    }

    private void SearchLostFocus(object? sender, RoutedEventArgs e) => SearchTextBox.Text = string.Empty;

    private FlyoutSearchItem GetOrCreateNode(ISearchFlyoutViewModel vm)
    {
        if (!_cachedNodes.TryPop(out var item))
            item = new(this);
        item.Selected = false;
        item.VM = vm;
        return item;
    }

    public void ReturnNode(FlyoutSearchItem node) => _cachedNodes.Push(node);
}