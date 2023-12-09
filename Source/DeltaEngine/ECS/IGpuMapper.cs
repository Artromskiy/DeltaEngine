namespace DeltaEngine.ECS;
internal interface IGpuMapper<T, K> where K : unmanaged
{
    K Map(T value);
}
