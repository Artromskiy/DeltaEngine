using DeltaEngine.Rendering;
using System;
using System.Collections.Generic;

namespace DeltaEngine.Files;
internal class MaterialCollection
{
    private readonly Dictionary<Guid, Dictionary<VertexAttribute, WeakReference<byte[]?>>> _meshMapVariants = new();
    private readonly Dictionary<Guid, WeakReference<ShaderData?>> _meshDataMap = new();
}
