using Delta.Rendering;
using System.Collections.Generic;
using System.Numerics;

namespace Delta.Files;

public class MaterialData : IAsset
{
    public readonly GuidAsset<ShaderData> shader;

    public Dictionary<string, float> _floatValues = new();
    public Dictionary<string, Vector2> _vector2Values = new();
    public Dictionary<string, Vector3> _vector3Values = new();
    public Dictionary<string, Vector4> _vector4Values = new();

    public MaterialData(GuidAsset<ShaderData> shader)
    {
        this.shader = shader;
    }
}
