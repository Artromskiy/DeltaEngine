using System;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace Delta.Files;

public interface IAsset { }

public readonly struct GuidAsset<T> : IEquatable<GuidAsset<T>>, IComparable<GuidAsset<T>> where T : class, IAsset
{
    public readonly Guid guid;

    public readonly T GetAsset() => AssetImporter.Instance.GetAsset(this);

    [JsonConstructor]
    internal GuidAsset(Guid guid)
    {
        this.guid = guid;
    }

    [MethodImpl(Inl)]
    public static implicit operator Guid(GuidAsset<T> guidAsset) => guidAsset.guid;

    [MethodImpl(Inl)]
    public readonly bool Equals(GuidAsset<T> other) => guid.Equals(other.guid);
    [MethodImpl(Inl)]
    public override bool Equals(object? obj) => obj is GuidAsset<T> asset && Equals(asset);
    [MethodImpl(Inl)]
    public override readonly int GetHashCode() => guid.GetHashCode();
    public readonly int CompareTo(GuidAsset<T> other) => guid.CompareTo(other.guid);

    [MethodImpl(Inl)]
    public static bool operator ==(GuidAsset<T> left, GuidAsset<T> right) => left.Equals(right);
    [MethodImpl(Inl)]
    public static bool operator !=(GuidAsset<T> left, GuidAsset<T> right) => !left.Equals(right);

}