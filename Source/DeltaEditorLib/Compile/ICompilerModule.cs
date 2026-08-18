using DVG.Engine.EditorLib.Scripting;
using System;
using System.Collections.Generic;

namespace DVG.Engine.EditorLib.Compile;

internal interface ICompilerModule
{
    public IAccessorsContainer? Accessors { get; }
    public List<Type> Components { get; }
    public void Recompile();
}
