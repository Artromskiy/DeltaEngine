using Arch.Core;
using Arch.Core.Extensions;
using Delta.ECS.Components;
using Delta.Runtime;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using static TransformAccessor;


object o = new Transform() { position = Vector3.Zero };
Transform tr = new Transform() { position = Vector3.One };
Vector3 pos = Vector3.UnitX;
ref var fieldRef = ref TransformAccessor.GetSetPosObj(ref o);
var fieldValue = fieldRef;
Console.WriteLine(fieldRef.GetType());
Console.WriteLine(fieldValue.GetType());

Console.WriteLine(fieldRef.ToString());
Console.WriteLine(fieldValue.ToString());

fieldRef = Vector3.One;

Console.WriteLine(fieldRef.GetType());
Console.WriteLine(fieldValue.GetType());

Console.WriteLine(fieldRef.ToString());
Console.WriteLine(fieldValue.ToString());

Console.WriteLine(fieldRef.GetType());
Console.WriteLine(o.ToString());
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
}

public interface IAccessorsContainer
{
    public Dictionary<Type, IAccessor> AllAccessors { get; }

    public FieldAccessor<O, T> GetFieldAccessor<O,T>(string name, ref O o)
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

static class AccessorsCommandGenerator<T>
{
    private static Dictionary<Type, SetCommand<T, object>> generatedCommands = [];
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

}

