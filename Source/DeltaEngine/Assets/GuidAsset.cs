using Delta.Runtime;
using System;
using System.Diagnostics;
using System.Text.Json.Serialization;

namespace Delta.Assets;

public interface IAsset { }

[DebuggerDisplay("guid = {guid}")]
public readonly struct GuidAsset<T> : IEquatable<GuidAsset<T>>, IComparable<GuidAsset<T>> where T : class, IAsset
{
    public readonly Guid guid;

    [JsonConstructor]
    internal GuidAsset(Guid guid) => this.guid = guid;

    public readonly T GetAsset() => IRuntimeContext.Current.AssetImporter.GetAsset(this);
    public override string ToString() => guid == Guid.Empty ? "null" : IRuntimeContext.Current.AssetImporter.GetName(this);

    [Imp(Inl)]
    public static implicit operator T(GuidAsset<T> guidAsset) => IRuntimeContext.Current.AssetImporter.GetAsset(guidAsset);

    [Imp(Inl)]
    public readonly int CompareTo(GuidAsset<T> other) => guid.CompareTo(other.guid);

    [Imp(Inl)]
    public override readonly int GetHashCode() => guid.GetHashCode(); // TODO override hashCode? just return first 32 bits?
    [Imp(Inl)]
    public readonly bool Equals(GuidAsset<T> other) => guid.Equals(other.guid);
    [Imp(Inl)]
    public override bool Equals(object? obj) => obj is GuidAsset<T> asset && Equals(asset);

    [Imp(Inl)]
    public static bool operator ==(GuidAsset<T> left, GuidAsset<T> right) => left.Equals(right);
    [Imp(Inl)]
    public static bool operator !=(GuidAsset<T> left, GuidAsset<T> right) => !left.Equals(right);

    public bool Null => guid == Guid.Empty;
}