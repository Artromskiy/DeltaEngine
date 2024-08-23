using Arch.Core;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;
using System.Diagnostics;

namespace DeltaEditor.Inspector.Nodes;

internal class GuidNode2: NodeWithPicker<Guid>
{
    private EntityReference cachedEntity;
    public GuidNode2(NodeData parameters, bool withName = true) : base(parameters, withName)
    {
        _fieldData.SelectedIndexChanged += OnValueChanged;
        ValueMode = FieldSizeMode.ExtraLarge;
    }

    public override bool UpdateData(EntityReference entity)
    {
        bool changed = false;
        cachedEntity = entity;
        Span<byte> guidBytes = stackalloc byte[16];
        GetData(entity).TryWriteBytes(guidBytes);
        _fieldData.SelectedItem = Convert.ToBase64String(guidBytes);
        return changed;
    }

    private void OnValueChanged(object? sender, EventArgs eventArgs)
    {
        if (!cachedEntity.IsAlive())
            return;
        _nodeData.rootData.RuntimeLoader.OnRuntimeThread += OpenFolder;
    }

    public void OpenFolder(IRuntime runtime)
    {
        string path = runtime.Context.AssetImporter.GetPath(GetData(cachedEntity));
        try
        {
            string? directory = Path.GetDirectoryName(path);
            if (Directory.Exists(directory))
                Process.Start("explorer.exe", directory);
        }
        catch { }
    }
}

internal abstract class NodeWithPicker<T> : Node<T>
{
    private readonly HorizontalStackLayout _stack;
    protected readonly Picker _fieldData = new()
    {
        MaximumHeightRequest = NodeHeight,
        MinimumHeightRequest = NodeHeight,
        Margin = 0,
    };

    public NodeWithPicker(NodeData parameters, bool withName) : base(parameters)
    {
        if (withName)
            _stack = [_fieldName, _fieldData];
        else
            _stack = [_fieldData];
        _stack.BackgroundColor = NodeConst.BackColor;
        _stack.VerticalOptions = new LayoutOptions(LayoutAlignment.Center, true);
        _stack.HorizontalOptions = new LayoutOptions(LayoutAlignment.Start, true);
        ValueMode = FieldSizeMode.Default;
        Content = _stack;
    }

    public FieldSizeMode ValueMode
    {
        set => _fieldData.MaximumWidthRequest = _fieldData.MinimumWidthRequest = SizeModeToSize(value);
    }

}