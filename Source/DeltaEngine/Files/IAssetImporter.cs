using System;
using System.Collections.Immutable;

namespace DeltaEngine.Files;
internal interface IAssetImporter
{
    public ImmutableHashSet<string> FileFormats { get; }
}
