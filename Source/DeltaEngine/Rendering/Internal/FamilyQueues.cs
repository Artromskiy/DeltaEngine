using Delta.Utilities;
using System;
using FamilyQueue = (uint family, uint queueNum);

namespace Delta.Rendering.Internal;

internal readonly struct FamilyQueues
{
    private readonly FamilyQueue?[] _familyQueues = new FamilyQueue?[Enums.GetCount<QueueType>()];
    public FamilyQueues() { }
    public FamilyQueues(Span<FamilyQueue?> familyQueues)
    {
        int length = familyQueues.Length;
        for (int i = 0; i < length; i++)
            _familyQueues[i] = familyQueues[i];
    }
    public FamilyQueues(Span<(int family, int queueNum)?> familyQueues)
    {
        int length = familyQueues.Length;
        for (int i = 0; i < length; i++)
            if (familyQueues[i].HasValue)
            {
                var family = (uint)familyQueues[i]!.Value.family;
                var queueNum = (uint)familyQueues[i]!.Value.queueNum;
                _familyQueues[i] = (family, queueNum);
            }
    }

    public FamilyQueue this[QueueType queueType]
    {
        get => _familyQueues[(int)queueType]!.Value;
        init => _familyQueues[(int)queueType] = value;
    }

    public bool HasQueue(QueueType queueType)
    {
        return _familyQueues[(int)queueType].HasValue;
    }

    public readonly int GetUniqueFamilies(Span<(uint family, int num)> uniqueFamilies)
    {
        Span<uint> avaliableFamilies = stackalloc uint[Enums.GetCount<QueueType>()];
        int count = GetAvaliableFamilies(avaliableFamilies);
        avaliableFamilies = avaliableFamilies[..count];
        return avaliableFamilies.CountRepetitions(uniqueFamilies);
    }

    private int GetAvaliableFamilies(Span<uint> families)
    {
        var values = Enums.GetValues<QueueType>();
        int length = values.Length;
        int count = 0;
        for (int i = 0; i < length; i++)
            if (HasQueue(values[i]))
                families[count++] = this[(QueueType)i].family;
        return count;
    }
}