using Delta.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace DeltaEditorLib.Scripting
{
    internal class CompileHelper(IProjectPath projectPath)
    {
        private const string CsSearch = "*.cs";
        private const string ScriptsDllName = "Scripts.dll";

        private const string AccessorsDllName = "Accessors.dll";

        private readonly IProjectPath _projectPath = projectPath;
        private readonly string dllPath = Path.Combine(projectPath.RootDirectory, ScriptsDllName);
        private string RandomDllName => Path.Combine(_projectPath.RootDirectory, Path.GetRandomFileName() + ScriptsDllName);


        private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp12);
        private static readonly CSharpCompilationOptions _compilationOptions = new
        (
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release,
            allowUnsafe: true
        );


        public string CompileScripts()
        {
            var sourceFiles = Directory.EnumerateFiles(_projectPath.RootDirectory, CsSearch, SearchOption.AllDirectories);
            var trees = sourceFiles.Select(x =>
            {
                using var stream = File.OpenRead(x);
                return CSharpSyntaxTree.ParseText(SourceText.From(stream), _parseOptions);
            });

            var references = GetReferences(trees);

            var compilation = CSharpCompilation.Create(ScriptsDllName, trees, references, _compilationOptions);

            var dllPath = RandomDllName;
            var result = compilation.Emit(dllPath);

            LogCompilation(result);

            return dllPath;
        }

        public string CompileAccessors(HashSet<Type> components)
        {
            AccessorGenerator generator = new();
            var code = generator.GenerateAccessors(components);
            SyntaxTree[] trees = [CSharpSyntaxTree.ParseText(SourceText.From(code), _parseOptions)];

            var references = GetReferences(trees);

            var compilation = CSharpCompilation.Create(AccessorsDllName, trees, references, _compilationOptions);

            var dllPath = RandomDllName;
            var result = compilation.Emit(dllPath);

            LogCompilation(result);

            return dllPath;
        }


        private List<MetadataReference> GetReferences(IEnumerable<SyntaxTree> trees)
        {
            MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference engineLib = MetadataReference.CreateFromFile(typeof(IRuntime).Assembly.Location);
            List<MetadataReference> references = [mscorlib, engineLib];

            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;
            references.AddRange(Assembly.
                GetEntryAssembly()!.
                GetReferencedAssemblies().
                Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            references.AddRange(trees.
                Select(tree => tree.GetRoot().ChildNodes().
                OfType<UsingDirectiveSyntax>().
                Where(x => x.Name != null).
                Select(x => x.Name)).
            SelectMany(s => s).
            Select(u => Path.Combine(assemblyPath, u!.ToString() + ".dll")).
            Where(File.Exists).
            Select(p => MetadataReference.CreateFromFile(p)));
            return references;
        }

        private static void LogCompilation(EmitResult result)
        {
            foreach (var item in result.Diagnostics)
                Debug.WriteLine(item.GetMessage());
        }
    }
}