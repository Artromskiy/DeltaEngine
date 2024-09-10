using Delta.Runtime;
using DeltaEditorLib.Loader;
using System.Diagnostics;

try
{
    string directoryPath = ProjectCreator.GetExecutableDirectory();
    var projectPath = new EditorPaths(directoryPath);
    ProjectCreator.CreateProject(projectPath);
    using var eng = new Runtime(projectPath);
    eng.Context.SceneManager.CreateTestScene();
    //eng.RuntimeCall += eng.Context.SceneManager.CreateTestScene;
    eng.Context.SceneManager.Running = true;

    //eng.RunOnce();
    Stopwatch sw = new();
    TimeSpan ms = TimeSpan.Zero;
    TimeSpan timer = TimeSpan.Zero;

    while (true)
    {
        Thread.Yield();
    }
}
catch (Exception e)
{
    Console.WriteLine(e);
}
Console.ReadLine();

public delegate ref R FieldAccessor<R, A>(ref A obj);
public interface IAccessor
{
    public FieldAccessor<Return, Access> GetFieldAccessor<Return, Access>(string name);
    public Type GetFieldType(string name);
    public object GetFieldValue(ref object obj, string name);
    public unsafe void* GetFieldPtr(void* ptr, string name);
}

public interface IAccessorsContainer
{
    public Dictionary<Type, IAccessor> AllAccessors { get; }

    public FieldAccessor<O, T> GetFieldAccessor<O, T>(string name, ref O o)
    {
        return default;
    }
}


public struct SetCommand<T, Field>
{
    public static IAccessorsContainer container;

    public static void Invoke(ref T obj, Queue<string> paths, object value)
    {
        if (paths.Count == 0)
        {
            obj = (T)value;
            return;
        }
        var path = paths.Dequeue();
        ref Field field = ref container.AllAccessors[typeof(T)].GetFieldAccessor<Field, T>(path).Invoke(ref obj);
        var fieldType = field.GetType();
        var command = AccessorsCommandGenerator<Field>.GetOrCreateCommand(fieldType);
        command.GetType().GetMethod("Invoke").Invoke(null, [field, paths, value]);
    }
}

internal static class AccessorsCommandGenerator<T>
{
    private static readonly Dictionary<Type, SetCommand<T, object>> generatedCommands = [];
    public static IAccessorsContainer container;

    public static void SetValue(ref T component, Queue<string> paths, object value)
    {
        var command = GetOrCreateCommand(typeof(T));
        command.GetType().GetMethod("Invoke").Invoke(null, [component, paths, value]);
    }

    public static object GetOrCreateCommand(Type t)
    {
        generatedCommands.TryGetValue(t, out var command);
        var genericObj = typeof(SetCommand<,>).MakeGenericType([typeof(T), t]);
        var obj = Activator.CreateInstance(genericObj);
        return obj;
    }

    private static unsafe void Set(Type t, void* ptr, Queue<string> paths, object value)
    {
        var path = paths.Dequeue();
        var fieldType = container.AllAccessors[t].GetFieldType(path);
        var fieldPtr = container.AllAccessors[t].GetFieldPtr(ptr, path);
        Set(fieldType, fieldPtr, paths, value);
        nint n = 10;
        n.ToPointer();
    }
    /*
    public static unsafe void* GetFieldPtr(void* ptr, string name)
    {
        var obj = Unsafe.AsRef<Transform>(ptr);
        return name switch
        {
            "position" => 
        }
    }
    */

}

