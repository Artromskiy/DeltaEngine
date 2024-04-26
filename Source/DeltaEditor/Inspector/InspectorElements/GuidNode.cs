using Arch.Core;
using DeltaEditor.Inspector.InspectorFields;
using System.Diagnostics;

namespace DeltaEditor.Inspector.InspectorElements
{
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
            OpenFolder(_nodeData.Context.AssetImporter.GetPath(GetData(cachedEntity)));
        }

        public void OpenFolder(string path)
        {
            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (Directory.Exists(directory))
                    Process.Start("explorer.exe", directory);
            }
            catch { }
        }
    }
}
