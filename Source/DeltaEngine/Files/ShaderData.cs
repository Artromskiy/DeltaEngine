using Delta.Files;
using System;
using System.Text.Json.Serialization;

namespace Delta.Rendering;

public class ShaderData : IAsset
{
    private readonly byte[] vertBytes;
    private readonly byte[] fragBytes;

    [JsonIgnore]
    public ReadOnlySpan<byte> VertBytes => vertBytes;
    [JsonIgnore]
    public ReadOnlySpan<byte> FragBytes => fragBytes;

    public ShaderData(byte[] vert, byte[] frag)
    {
        vertBytes = (byte[])vert.Clone();
        fragBytes = (byte[])frag.Clone();
    }
}