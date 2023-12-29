using System;
using System.Runtime.CompilerServices;

namespace Delta.Files;

public interface IAsset { }

public readonly struct GuidAsset<T> : IEquatable<GuidAsset<T>> where T : class, IAsset
{
    public readonly Guid guid;

    public readonly T Asset => AssetImporter.Instance.GetAsset(this);

    internal GuidAsset(Guid guid)
    {
        this.guid = guid;
    }

    [MethodImpl(Inl)]
    public bool Equals(GuidAsset<T> other) => guid.Equals(other.guid);
    [MethodImpl(Inl)]
    public override bool Equals(object? obj) => obj is GuidAsset<T> asset && Equals(asset);
    [MethodImpl(Inl)]
    public override readonly int GetHashCode() => guid.GetHashCode();
    [MethodImpl(Inl)]
    public static bool operator ==(GuidAsset<T> left, GuidAsset<T> right) => left.Equals(right);
    [MethodImpl(Inl)]
    public static bool operator !=(GuidAsset<T> left, GuidAsset<T> right) => !left.Equals(right);
}