using Delta.Runtime;
using System;
using System.Text.Json.Serialization;

namespace Delta.Files;

public interface IAsset { }
public interface IGuid { Guid GetGuid(); }

public readonly struct GuidAsset<T> : IGuid, IEquatable<GuidAsset<T>>, IComparable<GuidAsset<T>> where T : class, IAsset
{
    public readonly Guid guid;

    [JsonConstructor]
    internal GuidAsset(Guid guid) => this.guid = guid;

    public readonly T GetAsset() => IRuntimeContext.Current.AssetImporter.GetAsset(this);
    public readonly Guid GetGuid() => guid;

    [Imp(Inl)]
    public static implicit operator T(GuidAsset<T> guidAsset) => IRuntimeContext.Current.AssetImporter.GetAsset(guidAsset);

    [Imp(Inl)]
    public readonly int CompareTo(GuidAsset<T> other) => guid.CompareTo(other.guid);

    [Imp(Inl)]
    public override readonly int GetHashCode() => guid.GetHashCode();
    [Imp(Inl)]
    public readonly bool Equals(GuidAsset<T> other) => guid.Equals(other.guid);
    [Imp(Inl)]
    public override bool Equals(object? obj) => obj is GuidAsset<T> asset && Equals(asset);

    [Imp(Inl)]
    public static bool operator ==(GuidAsset<T> left, GuidAsset<T> right) => left.Equals(right);
    [Imp(Inl)]
    public static bool operator !=(GuidAsset<T> left, GuidAsset<T> right) => !left.Equals(right);

}