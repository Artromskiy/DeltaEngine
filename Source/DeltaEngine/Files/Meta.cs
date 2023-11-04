using System;
using System.Text.Json.Serialization;

namespace DeltaEngine.Files;
internal readonly struct Meta
{
    [JsonInclude]
    public readonly Guid guid;
    //public readonly UInt128 checkSum;
    //public readonly Dictionary<string, object> metaAdditions;

    [JsonConstructor]
    public Meta(Guid guid)
    {
        this.guid = guid;
        //this.checkSum = checkSum;
    }
}