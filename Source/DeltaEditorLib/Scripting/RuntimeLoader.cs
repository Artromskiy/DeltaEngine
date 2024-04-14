using Arch.Core.Extensions;
using Delta.ECS.Components;
using Delta.Runtime;
using Delta.Scripting;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace DeltaEditorLib.Scripting
{
    public class RuntimeLoader
    {
        private readonly IProjectPath _projectPath;
        private readonly CompileHelper _compileHelper;

        private AssemblyLoadContext LoadContext;
        private AssemblyLoadContext.ContextualReflectionScope _scope;

        private readonly HashSet<WeakReference<AssemblyLoadContext>> _oldAlcs = [];
        private readonly HashSet<Type> _components = [];

        private readonly HashSet<Func<Task>> UICallLoopTasks = [];
        public event Func<Task> OnUICallLoop
        {
            add => UICallLoopTasks.Add(value);
            remove => UICallLoopTasks.Remove(value);
        }

        public IRuntime Runtime { get; private set; }
        public IAccessorsContainer AccessorsContainer { get; private set; }

        public RuntimeLoader(IProjectPath projectPath)
        {
            _projectPath = projectPath;
            _compileHelper = new(_projectPath);

            LoadContext = NewLoadContext();
            Compile();

            _components.UnionWith(GetComponents());
            AccessorsContainer = (Activator.CreateInstance(AccessorsContainerType()) as IAccessorsContainer)!;

            Runtime = new Runtime(_projectPath);
            Runtime.RuntimeCall += RuntimeLoop;
        }

        public void ReloadRuntime()
        {
            Runtime.Dispose();
            Runtime = null;
            UnloadContext();
            LoadContext = NewLoadContext();
            Compile();

            _components.UnionWith(GetComponents());
            AccessorsContainer = (Activator.CreateInstance(AccessorsContainerType()) as IAccessorsContainer)!;

            Runtime = new Runtime(_projectPath);
            Runtime.RuntimeCall += RuntimeLoop;
            return;
        }

        private void Compile()
        {
            var scriptingDll = _compileHelper.CompileScripts();
            LoadContext.LoadFromAssemblyPath(scriptingDll);
            HashSet<Type> components = new(GetComponents());
            var accessorsDll = _compileHelper.CompileAccessors(components);
            LoadContext.LoadFromAssemblyPath(accessorsDll);
        }

        private AssemblyLoadContext NewLoadContext()
        {
            var loadContext = new AssemblyLoadContext("Scripting", true);
            _scope = loadContext.EnterContextualReflection();
            return loadContext;
        }

        private void UnloadContext()
        {
            _components.Clear();
            AccessorsContainer = null;
            _scope.Dispose();
            _scope = default;
            LoadContext?.Unload();
            if (LoadContext != null)
                _oldAlcs.Add(new WeakReference<AssemblyLoadContext>(LoadContext, false));
            LoadContext = null;
            _oldAlcs.RemoveWhere(r => !r.TryGetTarget(out _));
        }


        public List<Type> Components => new(_components);

        private static Type AccessorsContainerType()
        {
            var contextAssemblies = AssemblyLoadContext.CurrentContextualReflectionContext!.Assemblies;
            var contextTypes = contextAssemblies.SelectMany(x => x.GetTypes());
            return contextTypes.Where(t => typeof(IAccessorsContainer).IsAssignableFrom(t)).FirstOrDefault();
        }

        private static IEnumerable<Type> GetComponents()
        {
            var contextAssemblies = AssemblyLoadContext.CurrentContextualReflectionContext!.Assemblies;
            var mainAssemblies = AssemblyLoadContext.Default.Assemblies;
            return contextAssemblies.Select(GetComponents).
                Concat(AssemblyLoadContext.Default.Assemblies.Select(GetComponents)).
                SelectMany(type => type);
        }

        private static IEnumerable<Type> GetComponents(Assembly assembly)
        {
            return assembly.GetTypes().
                Where(type => type.GetCustomAttribute<ComponentAttribute>() != null);
        }

        public unsafe void CheckAccessors()
        {
            using var _ = Runtime.Pause;
            var withTransform = Runtime.GetEntities().Where(e => e.Entity.Has<Transform>());
            foreach (var item in withTransform)
            {
                ref var tr = ref item.Entity.Get<Transform>();
                var ptr = new nint(Unsafe.AsPointer(ref tr));
                Queue<string> queue = new(["position", "X"]);
                Set<float>(tr.GetType(), ptr, queue, 10);
            }
        }

        private unsafe void Set<T>(Type type, nint ptr, Queue<string> paths, T value)
        {
            if (paths.Count == 0)
            {
                if (typeof(T) == type)
                    Unsafe.AsRef<T>(ptr.ToPointer()) = value;
                return;
            }
            var path = paths.Dequeue();
            var accessor = AccessorsContainer.AllAccessors[type];
            ptr = accessor.GetFieldPtr(ptr, path);
            type = accessor.GetFieldType(path);
            Set(type, ptr, paths, value);
        }

        private bool _runtimeRunning;
        public bool RuntimeRunning
        {
            set
            {
                if (_runtimeRunning = value)
                    Runtime.Running = true;
            }
        }

        private async void RuntimeLoop()
        {
            Runtime.Running = false;
            try
            {
                if (UICallLoopTasks.Count != 0)
                    await Task.WhenAll(UICallLoopTasks.Select(t => t.Invoke())).ContinueWith((x) => { Runtime.Running = _runtimeRunning; });
                else
                    Runtime.Running = _runtimeRunning;
            }
            catch
            {
                Runtime.Running = _runtimeRunning;
            }
        }

        public void OpenProjectFolder()
        {
            try
            {
                if (Directory.Exists(_projectPath.RootDirectory))
                    Process.Start("explorer.exe", _projectPath.RootDirectory);
            }
            catch { }
        }
    }

}