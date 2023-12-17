﻿using DeltaEngine.ECS;
using DeltaEngine.Files;
using DeltaEngine.Rendering;
using System.Runtime.CompilerServices;

namespace DeltaEngine;

internal struct Render : IDirty
{
    internal GuidAsset<ShaderData> _shader;
    internal GuidAsset<MaterialData> _material;
    public GuidAsset<MeshData> Mesh;

    public readonly GuidAsset<ShaderData> Shader
    {
        [MethodImpl(Inl)]
        get => _shader;
    }

    public GuidAsset<MaterialData> Material
    {
        [MethodImpl(Inl)]
        readonly get => _material;
        [MethodImpl(Inl)]
        set
        {
            _material = value;
            _shader = value.Asset.shader;
        }
    }
}
