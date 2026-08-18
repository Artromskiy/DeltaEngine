using System;
using System.Text.Json.Serialization;

namespace DVG.Engine.Assets;

[method: JsonConstructor]
internal readonly struct Meta(Guid guid, int version)
{
    [JsonInclude]
    public readonly Guid guid = guid;
    [JsonInclude]
    public readonly int version = version;
}
