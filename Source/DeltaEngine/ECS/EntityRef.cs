using Arch.Core;
using System.Runtime.CompilerServices;

namespace Delta.ECS;

public readonly struct EntityRef
{
    private readonly EntityReference _entityReference;
    private readonly Entity Entity
    {
        [MethodImpl(Inl)]
        get => _entityReference.Entity;
    }

    public readonly int Version
    {
        [MethodImpl(Inl)]
        get => _entityReference.Version;
    }

    public static readonly EntityRef Null = new(EntityReference.Null);
    internal EntityRef(EntityReference entityReference) => _entityReference = entityReference;

    [MethodImpl(Inl)]
    public readonly bool IsAlive() => _entityReference.IsAlive();
    [MethodImpl(Inl)]
    public readonly bool Equals(EntityRef other) => _entityReference.Equals(other._entityReference);
    [MethodImpl(Inl)]
    public override readonly bool Equals(object? obj) => obj is EntityRef other && Equals(other);
    [MethodImpl(Inl)]
    public override readonly int GetHashCode() => _entityReference.GetHashCode();
    [MethodImpl(Inl)]
    public static bool operator ==(EntityRef left, EntityRef right) => left.Equals(right);
    [MethodImpl(Inl)]
    public static bool operator !=(EntityRef left, EntityRef right) => !left.Equals(right);
    [MethodImpl(Inl)]
    public static implicit operator Entity(EntityRef reference) => reference.Entity;

    public override string ToString() => $"id: {_entityReference.Entity.Id}, ver: {_entityReference.Version}";
}