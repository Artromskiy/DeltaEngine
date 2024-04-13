using Arch.Core;
using Delta.Runtime;
using Delta.Scripting;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.Loader;

namespace DeltaEditorLib.Scripting
{
    public class RuntimeLoader
    {
        private readonly IProjectPath _projectPath;

        private readonly List<object> _instantiatedObjects = [];

        private AssemblyLoadContext LoadContext;
        private AssemblyLoadContext.ContextualReflectionScope _scope;
        private readonly CompileHelper _compileHelper;

        private readonly HashSet<WeakReference<AssemblyLoadContext>> _oldAlcs = [];

        private readonly HashSet<Type> _components = [];

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