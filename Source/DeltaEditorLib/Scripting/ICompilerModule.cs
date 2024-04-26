namespace DeltaEditorLib.Scripting;

internal interface ICompilerModule
{
    public IAccessorsContainer? Accessors { get; }
    public List<Type> Components { get; }
    public void Recompile();
}
