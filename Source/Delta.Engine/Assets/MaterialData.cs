using System.Collections.Generic;
using Delta.Maths;
using System.Text.Json.Serialization;

namespace Delta.Engine.Assets;

public class MaterialData : IAsset
{
    public readonly GuidAsset<ShaderData> shader;

    [JsonIgnore]
    public Dictionary<string, float> _floatValues = [];
    [JsonIgnore]
    public Dictionary<string, float2> _vector2Values = [];
    [JsonIgnore]
    public Dictionary<string, float3> _vector3Values = [];
    [JsonIgnore]
    public Dictionary<string, float4> _vector4Values = [];

    public MaterialData(GuidAsset<ShaderData> shader)
    {
        this.shader = shader;
    }
}
