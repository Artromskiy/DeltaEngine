using Delta.Rendering;
using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Delta.Files;

public class MaterialData : IAsset
{
    public readonly GuidAsset<ShaderData> shader;

    public Dictionary<string, float> _floatValues = [];
    public Dictionary<string, Vector2> _vector2Values = [];
    public Dictionary<string, Vector3> _vector3Values = [];
    public Dictionary<string, Vector4> _vector4Values = [];

    public MaterialData(GuidAsset<ShaderData> shader)
    {
        this.shader = shader;
    }
    [JsonConstructor]
    private MaterialData() { }
}
