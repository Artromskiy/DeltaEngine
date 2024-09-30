using System;
using System.Text.Json.Serialization;

namespace Delta.Assets;

public class ShaderData : IAsset
{
    private readonly byte[] vert;
    private readonly byte[] frag;

    public ReadOnlySpan<byte> GetVertBytes() => vert;
    public ReadOnlySpan<byte> GetFragBytes() => frag;

    [JsonConstructor]
    public ShaderData(byte[] vert, byte[] frag)
    {
        this.vert = (byte[])vert.Clone();
        this.frag = (byte[])frag.Clone();
    }
}