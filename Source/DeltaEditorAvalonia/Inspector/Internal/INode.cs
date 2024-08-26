﻿using Arch.Core;

namespace DeltaEditorAvalonia.Inspector.Internal;

internal interface INode
{
    /// <summary>
    /// Updates data from engine to editor or from editor to engine
    /// </summary>
    /// <param name="entity"></param>
    /// <param name="changed"></param>
    /// <returns>true if data was modified from editor</returns>
    public bool UpdateData(EntityReference entity);
}