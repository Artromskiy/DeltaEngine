using DeltaEditorLib.Scripting;
using System;
using System.Collections.Generic;

namespace DeltaEditorLib.Compile;

internal interface ICompilerModule
{
    public IAccessorsContainer? Accessors { get; }
    public List<Type> Components { get; }
    public void Recompile();
}
