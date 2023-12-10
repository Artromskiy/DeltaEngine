namespace DeltaEngine.ECS;
internal interface IGpuMapper<T, K> where K : unmanaged
{
    K Map(ref T value);
}
