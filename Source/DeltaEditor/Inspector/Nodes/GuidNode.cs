using Arch.Core;
using Delta.Runtime;
using DeltaEditor.Inspector.Internal;
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

    public override void UpdateData(EntityReference entity)
    {
        cachedEntity = entity;
        Span<byte> guidBytes = stackalloc byte[16];
        GetData(entity).TryWriteBytes(guidBytes);
        _fieldData.Text = Convert.ToBase64String(guidBytes);
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
