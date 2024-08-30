using Delta.Utilities;
using System;

namespace Delta.Rendering.Internal;

internal readonly record struct QueueFamilies
{
    public readonly (uint family, uint queueNum) graphics { get; init; }
    public readonly (uint family, uint queueNum) present { get; init; }
    public readonly (uint family, uint queueNum) compute { get; init; }
    public readonly (uint family, uint queueNum) transfer { get; init; }

    public readonly int GetUniqueFamilies(Span<(uint family, uint num)> uniqueFamilies)
    {
        Span<uint> families = [graphics.family, present.family, compute.family, transfer.family];
        int count = families.Distinct();
        for (int i = 0; i < count; i++)
        {
            var item = families[i];
            uint gr = item == graphics.family && graphics.queueNum > 0 ? 1u : 0u;
            uint pr = item == present.family && present.queueNum > 0 ? 1u : 0u;
            uint cm = item == compute.family && compute.queueNum > 0 ? 1u : 0u;
            uint tr = item == transfer.family && transfer.queueNum > 0 ? 1u : 0u;
            uniqueFamilies[i] = (item, 1 + gr + pr + cm + tr);
        }
        return count;
    }

}
