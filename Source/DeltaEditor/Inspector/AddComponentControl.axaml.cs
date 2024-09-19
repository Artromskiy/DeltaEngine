using Avalonia.Controls;
using Avalonia.Interactivity;
using DeltaEditor.Hierarchy;
using System;
using Avalonia.Input;
using System.Collections.Generic;

namespace DeltaEditor;

public partial class AddComponentControl : UserControl
{
    private readonly Dictionary<Type, AddComponentItemControl> _createdItems = [];
    private readonly HashSet<Type> _currentTypes = [];
    private string? _searchString;
    private int _selectedNodeIndex;

    public event Action<Type> OnComponentAddRequested;
    private IListWrapper<AddComponentItemControl, Control> ChildrenNodes => new(ComponentsStackPanel.Children);


    public AddComponentControl() => InitializeComponent();

    public void FillComponentsData(HashSet<Type> types)
    {
        _currentTypes.Clear();
        _currentTypes.UnionWith(types);
        UpdateNodes();
    }

    private AddComponentItemControl GetOrCreateItem(Type type)
    {
        if (!_createdItems.TryGetValue(type, out var item))
            _createdItems[type] = item = new(type, OnComponentAddRequested);
        item.Selected = false;
        return item;
    }

    private void UpdateNodes()
    {
        _selectedNodeIndex = 0;
        ChildrenNodes.Clear();
        foreach (var type in _currentTypes)
            if (string.IsNullOrEmpty(_searchString) || type.ToString().Contains(_searchString, StringComparison.InvariantCultureIgnoreCase))
                ChildrenNodes.Add(GetOrCreateItem(type));
        if (ChildrenNodes.Count != 0)
            ChildrenNodes[_selectedNodeIndex].Selected = true;
    }

    private void SearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ChildrenNodes.Count != 0)
        {
            OnComponentAddRequested?.Invoke(ChildrenNodes[_selectedNodeIndex].ComponentType);
            SearchTextBox.Text = string.Empty;
        }
        else if (e.Key == Key.Down)
        {
            _selectedNodeIndex = int.Min(++_selectedNodeIndex, ChildrenNodes.Count - 1);
            UpdateSelection();
        }
        else if (e.Key == Key.Up)
        {
            _selectedNodeIndex = int.Max(0, --_selectedNodeIndex);
            UpdateSelection();
        }
    }

    private void UpdateSelection()
    {
        foreach (var item in ChildrenNodes)
            item.Selected = false;
        if(ChildrenNodes.Count != 0)
        ChildrenNodes[_selectedNodeIndex].Selected = true;
    }

    private void SearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchString = SearchTextBox.Text;
        UpdateNodes();
    }

    private void SearchLostFocus(object? sender, RoutedEventArgs e)=> SearchTextBox.Text = string.Empty;
    private void Opened(object? sender, GotFocusEventArgs e)
    {
        UpdateNodes();
        SearchTextBox.Focus();
    }
}