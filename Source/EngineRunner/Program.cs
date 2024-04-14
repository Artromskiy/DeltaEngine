using Delta.Runtime;
using System.Diagnostics;



//try
{
    using var eng = new Runtime(new EditorPaths(Directory.GetCurrentDirectory()));
    eng.CreateTestScene();
    eng.RunOnce();
    Stopwatch sw = new();

    int c = 0;
    TimeSpan ms = TimeSpan.Zero;
    TimeSpan timer = TimeSpan.Zero;

    while (true)
    {
        Thread.Yield();
        sw.Restart();
        eng.RunOnce();
        sw.Stop();
        ms += sw.Elapsed;
        timer += sw.Elapsed;
        c++;
        if (c == 100)
        {
            var el = ms / c;
            Console.WriteLine();
            Console.WriteLine((int)(1.0 / el.TotalSeconds)); // FPS of main thread
            Console.WriteLine();
            foreach (var item in eng.GetCurrentScene().GetMetrics())
                Console.WriteLine($"{item.Key}: {(item.Value / 100).TotalMilliseconds:0.00}ms");
            eng.GetCurrentScene().ClearMetrics();
            ms = TimeSpan.Zero;
            c = 0;
        }
        if (timer.TotalSeconds >= 20)
        {
            timer = TimeSpan.Zero;
            eng.CreateTestScene();
        }
    }
}
//catch (Exception e)
//{
//    Console.WriteLine(e);
//}
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

