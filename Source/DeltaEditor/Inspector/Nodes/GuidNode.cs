using Arch.Core;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;
using DeltaEditor.Tools;
using System.Diagnostics;

namespace DeltaEditor.Inspector.Nodes;

internal class GuidNode : ClickableNode<Guid>
{
    private EntityReference cachedEntity;
    public GuidNode(NodeData parameters, bool withName = true) : base(parameters, withName)
    {
        _fieldData.Clicked += OnClick;
        ValueMode = FieldSizeMode.ExtraLarge;
    }

    public override bool UpdateData(EntityReference entity)
    {
        cachedEntity = entity;
        _fieldData.Text = GetData(entity).LookupString();
        return false;
    }

    private void OnClick(object? sender, EventArgs eventArgs)
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
