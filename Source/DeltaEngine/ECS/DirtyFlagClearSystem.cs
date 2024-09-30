using Arch.Core;
using Arch.Core.Utils;
using Delta.ECS.Attributes;
using Delta.ECS.Components;
using Delta.Runtime;
using Delta.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Delta.ECS;
internal static class DirtyFlagClearSystem
{
    private static readonly Dictionary<Type, IGenericRemoveWrapper> _removers = [];
    public static void Execute()
    {
        foreach (var type in ComponentRegistry.Types)
        {
            if (type == null || !AttributeCache.HasAttribute<DirtyAttribute>(type))
                continue;
            if (!_removers.TryGetValue(type, out var remover))
                if (Activator.CreateInstance(typeof(GenericRemoveWrapper<>).MakeGenericType(type)) is IGenericRemoveWrapper iRemover)
                    _removers[type] = remover = iRemover;
            Debug.Assert(remover != null);
            remover.Remove();
        }
    }

    private readonly struct GenericRemoveWrapper<T> : IGenericRemoveWrapper
    {
        private static readonly QueryDescription _removeDescription = new QueryDescription().WithAll<DirtyFlag<T>>();
        public readonly void Remove()
        {
            IRuntimeContext.Current.SceneManager.CurrentScene._world.Remove<DirtyFlag<T>>(_removeDescription);
        }
    }
    private interface IGenericRemoveWrapper
    {
        public void Remove();
    }
}
