using Arch.Core;
using Arch.Core.Extensions;
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

        private AssemblyLoadContext _alc;
        private AssemblyLoadContext.ContextualReflectionScope _scope;
        private readonly CompileHelper _compileHelper;

        private readonly HashSet<WeakReference<AssemblyLoadContext>> _oldAlcs = [];

        private readonly HashSet<Type> _components = [];

        public IRuntime Runtime { get; private set; }

        public RuntimeLoader(IProjectPath projectPath)
        {
            _projectPath = projectPath;
            _compileHelper = new(_projectPath);

            _alc = Recompile();

            Runtime = new Runtime(_projectPath);
        }

        private AssemblyLoadContext Recompile()
        {
            var scriptingDll = _compileHelper.CompileScripts();

            _scope.Dispose();
            _alc?.Unload();

            _oldAlcs.RemoveWhere(r => !r.TryGetTarget(out _));

            if (_alc != null)
                _oldAlcs.Add(new WeakReference<AssemblyLoadContext>(_alc, false));

            var alc = new AssemblyLoadContext("Scripting", true);
            _scope = alc.EnterContextualReflection();
            alc.LoadFromAssemblyPath(scriptingDll);

            _components.Clear();
            _components.UnionWith(GetComponents());

            var accessorsDll = _compileHelper.CompileAccessors(_components);
            var accessorsAssembly = alc.LoadFromAssemblyPath(accessorsDll);
            var accessorType = accessorsAssembly.GetTypes().Where(t => typeof(IAccessorsContainer).IsAssignableFrom(t)).FirstOrDefault();
            var accessor = Activator.CreateInstance(accessorType) as IAccessorsContainer;

            return alc;
        }

        public void ReloadRuntime()
        {
            Runtime.Running = false;

            _alc = Recompile();

            Runtime.Dispose();
            Runtime = new Runtime(_projectPath);
            return;
        }

        public List<Type> Components => new(_components);
        public List<EntityReference> GetEntities() => Runtime.GetEntities();


        private void ConvertEntityToJson()
        {
            Entity entity = Entity.Null;
            var components = entity.GetComponentTypes();
            //jsonchema
            //components[0].;
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