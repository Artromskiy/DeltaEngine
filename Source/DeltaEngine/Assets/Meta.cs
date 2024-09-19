using System;
using System.Text.Json.Serialization;

namespace Delta.Assets;

[method: JsonConstructor]
internal readonly struct Meta(Guid guid, int version)
{
    [JsonInclude]
    public readonly Guid guid = guid;
    [JsonInclude]
    public readonly int version = version;
}