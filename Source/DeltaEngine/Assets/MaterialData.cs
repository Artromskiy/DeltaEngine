using System.Collections.Generic;
using System.Numerics;
using System.Text.Json.Serialization;

namespace Delta.Assets;

public class MaterialData : IAsset
{
    public readonly GuidAsset<ShaderData> shader;

    [JsonIgnore]
    public Dictionary<string, float> _floatValues = [];
    [JsonIgnore]
    public Dictionary<string, Vector2> _vector2Values = [];
    [JsonIgnore]
    public Dictionary<string, Vector3> _vector3Values = [];
    [JsonIgnore]
    public Dictionary<string, Vector4> _vector4Values = [];

    public MaterialData(GuidAsset<ShaderData> shader)
    {
        this.shader = shader;
    }
}
