namespace Delta.ECS;
internal struct VersId<T>(uint id, uint vers)
{
    internal uint id = id;
    internal uint vs = vers;
}