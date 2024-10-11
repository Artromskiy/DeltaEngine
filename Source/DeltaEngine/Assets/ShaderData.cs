using Delta.Rendering;
using System;
using System.Text.Json.Serialization;

namespace Delta.Assets;

public class ShaderData : IAsset
{
    public readonly VertexAttribute attributeMask;
    private readonly byte[] vert;
    private readonly byte[] frag;

    public ReadOnlySpan<byte> GetVertBytes() => vert;
    public ReadOnlySpan<byte> GetFragBytes() => frag;

    [JsonConstructor]
    public ShaderData(byte[] vert, byte[] frag, VertexAttribute attributeMask)
    {
        this.vert = (byte[])vert.Clone();
        this.frag = (byte[])frag.Clone();
        this.attributeMask = attributeMask;
    }
}