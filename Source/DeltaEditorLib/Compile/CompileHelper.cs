using Delta.Runtime;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Reflection;

namespace DeltaEditorLib.Compile
{
    internal class CompileHelper(IProjectPath projectPath)
    {
        private const string CsSearch = "*.cs";
        private const string dllExt = ".dll";
        private const string Scripts = "Scripts";
        private const string Accessors = "Accessors";
        private const string ScriptsPathSuffix = Scripts + dllExt;
        private const string AccessorsPathSuffix = Accessors + dllExt;

        private readonly IProjectPath _projectPath = projectPath;
        private string RandomScriptsDllName => Path.Combine(_projectPath.RootDirectory, Path.GetRandomFileName() + ScriptsPathSuffix);
        private string RandomAccessorsDllName => Path.Combine(_projectPath.RootDirectory, Path.GetRandomFileName() + AccessorsPathSuffix);
        private string RandomPdbName => Path.Combine(_projectPath.RootDirectory, Path.GetRandomFileName() + ".pdb");

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

            var compilation = CSharpCompilation.Create(Scripts, trees, references, _compilationOptions);

            var dllPath = RandomScriptsDllName;
            var result = compilation.Emit(dllPath, RandomPdbName);

            LogCompilation(result);

            return dllPath;
        }

        public string CompileAccessors(HashSet<Type> components)
        {
            var code = AccessorGenerator.GenerateAccessors(components);
            SyntaxTree[] trees = [CSharpSyntaxTree.ParseText(SourceText.From(code), _parseOptions)];

            var references = GetReferences(trees);

            var compilation = CSharpCompilation.Create(Accessors, trees, references, _compilationOptions);

            var dllPath = RandomAccessorsDllName;
            var result = compilation.Emit(dllPath, RandomPdbName);

            LogCompilation(result);

            return dllPath;
        }

        private static List<MetadataReference> GetReferences(IEnumerable<SyntaxTree> trees)
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
                Debug.Assert(false, item.GetMessage());
        }
    }
}