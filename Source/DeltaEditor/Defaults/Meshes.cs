using Delta.Files;

namespace DeltaEditor.Defaults
{
    internal class Meshes
    {
        private const string ArrowPath = "Resources/Meshes/arrow.mesh";

        public static MeshData ArrowMesh => Serialization.Deserialize<MeshData>(ArrowPath);
    }
}
