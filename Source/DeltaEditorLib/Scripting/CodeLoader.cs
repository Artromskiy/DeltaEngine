using Delta;
using Delta.Scripting;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Diagnostics;
using System.Reflection;

namespace DeltaEditorLib.Scripting
{
    public static class CodeLoader
    {
        private static readonly CSharpParseOptions _parseOptions = new(LanguageVersion.CSharp12);
        private static readonly CSharpCompilationOptions _compilationOptions = new
        (
            OutputKind.DynamicallyLinkedLibrary,
            optimizationLevel: OptimizationLevel.Release
        );
        private const string DllName = "Scripts.dll";

        public static void TryCompile(string sourceFilesLocation, string destinationDllLocation)
        {
            TestCompileFiles.CreateTestFile(sourceFilesLocation);

            DirectoryInfo d = new(sourceFilesLocation);
            string[] sourceFiles = Directory.GetFiles(sourceFilesLocation, "*.cs", SearchOption.AllDirectories);

            List<SyntaxTree> trees = new(sourceFiles.Length);
            foreach (string file in sourceFiles)
            {
                using var stream = File.OpenRead(file);
                SyntaxTree tree = CSharpSyntaxTree.ParseText(SourceText.From(stream), _parseOptions);
                trees.Add(tree);
            }

            MetadataReference mscorlib = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
            MetadataReference engineLib = MetadataReference.CreateFromFile(typeof(Engine).Assembly.Location);

            string assemblyPath = Path.GetDirectoryName(typeof(object).Assembly.Location)!;

            List<MetadataReference> references = [mscorlib, engineLib];

            references.AddRange(Assembly.
                GetEntryAssembly()!.
                GetReferencedAssemblies().
                Select(a => MetadataReference.CreateFromFile(Assembly.Load(a).Location)));

            references.AddRange(trees.
                Select(tree => tree.GetRoot().ChildNodes().
                OfType<UsingDirectiveSyntax>().
                Where(x => x.Name != null)).
            SelectMany(s => s).
            Select(u => Path.Combine(assemblyPath, u.Name!.ToString() + ".dll")).
            Where(File.Exists).
            Select(p => MetadataReference.CreateFromFile(p)));

            var compilation = CSharpCompilation.Create(DllName,
                   trees,
                   references,
                   _compilationOptions);

            var dllPath = Path.Combine(destinationDllLocation, DllName);

            var result = compilation.Emit(dllPath);

            foreach (var item in result.Diagnostics)
                Debug.WriteLine(item.GetMessage());

            var scriptsDllReference = MetadataReference.CreateFromFile(dllPath);

            var assembly = Assembly.LoadFrom(dllPath);
            var types = GetComponentTypes(assembly);

            foreach (var item in types)
                Debug.WriteLine(item.FullName);
        }

        private static List<Type> GetComponentTypes(Assembly assembly) => assembly.
            GetTypes().
            Where(type => type.GetCustomAttribute<ComponentAttribute>() != null).
            ToList();
    }
}