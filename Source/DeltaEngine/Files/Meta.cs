using System;
using System.Text.Json.Serialization;

namespace Delta.Files;

[method: JsonConstructor]
internal readonly struct Meta(Guid guid)
{
    [JsonInclude]
    public readonly Guid guid = guid;
}