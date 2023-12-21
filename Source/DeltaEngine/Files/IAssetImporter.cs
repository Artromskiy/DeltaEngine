using System.Collections.Immutable;

namespace Delta.Files;
internal interface IAssetImporter
{
    public ImmutableHashSet<string> FileFormats { get; }
}
