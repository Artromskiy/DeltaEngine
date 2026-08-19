using Delta.Editor.Scripting;
using Delta.Engine.Integration;
using Delta.Maths;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Xunit;

namespace Delta.Engine.Integration.Tests;

public sealed class VerticalSliceContractTests
{
    [Fact]
    public void ScriptCompiler_returns_assembly_bytes_on_success()
    {
        var compiler = new RoslynScriptCompiler();
        var result = compiler.Compile(new ScriptCompilationRequest(
            [new ScriptSource("Good.cs", "public sealed class Good { public int Value; }")],
            RuntimeReferences()));

        Assert.True(result.Success);
        Assert.NotEmpty(result.AssemblyBytes.ToArray());
        Assert.NotEmpty(result.PdbBytes.ToArray());
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == ScriptDiagnosticSeverity.Error);
    }

    [Fact]
    public void ScriptCompiler_returns_diagnostics_without_assembly_on_failure()
    {
        var compiler = new RoslynScriptCompiler();
        var result = compiler.Compile(new ScriptCompilationRequest(
            [new ScriptSource("Broken.cs", "public sealed class Broken {")],
            RuntimeReferences()));

        Assert.False(result.Success);
        Assert.Empty(result.AssemblyBytes.ToArray());
        Assert.Empty(result.PdbBytes.ToArray());
        var diagnostic = Assert.Single(result.Diagnostics, item => item.Severity == ScriptDiagnosticSeverity.Error);
        Assert.Equal("CS1513", diagnostic.Id);
        Assert.Equal("Broken.cs", diagnostic.SourcePath);
        Assert.True(diagnostic.Line.HasValue);
    }

    [Fact]
    public void ComponentSchema_contains_nested_editable_string_and_maths_fields()
    {
        ComponentSchema schema = ComponentSchemaBuilder.Create<TransformComponent>();

        var position = Assert.Single(schema.Fields, field => field.Name == nameof(TransformComponent.Position));
        var x = Assert.Single(position.Children, field => field.Name == nameof(float3.x));
        var privateField = Assert.Single(schema.Fields, field => field.Name == "_speed");
        var nested = Assert.Single(schema.Fields, field => field.Name == nameof(TransformComponent.Nested));
        var nestedPrivateField = Assert.Single(nested.Children, field => field.Name == "_weight");

        Assert.Equal("Position.x", x.Id);
        Assert.Equal(typeof(float), x.FieldType);
        Assert.Equal(typeof(float3), position.FieldType);
        Assert.Equal(ComponentFieldAccess.Read | ComponentFieldAccess.Write, privateField.Access);
        Assert.Contains(privateField.Attributes, attribute => attribute.EndsWith("EditableAttribute", StringComparison.Ordinal));
        Assert.Equal("Nested._weight", nestedPrivateField.Id);
    }

    [Fact]
    public void AccessorTree_reads_and_writes_nested_managed_and_editable_values()
    {
        var component = new TransformComponent
        {
            Position = new float3(1, 2, 3),
            Label = "before",
        };
        var accessors = ComponentAccessorTree.Create<TransformComponent>();

        Assert.True(accessors.TrySet(component, "Position.x", 9f));
        Assert.True(accessors.TrySet(component, "Label", "after"));
        Assert.True(accessors.TrySet(component, "_speed", 4));
        Assert.True(accessors.TrySet(component, "Nested._weight", 2.5f));
        Assert.True(accessors.TryGet(component, "Position.x", out var x));
        Assert.True(accessors.TryGet(component, "Label", out var label));
        Assert.True(accessors.TryGet(component, "_speed", out var speed));
        Assert.True(accessors.TryGet(component, "Nested._weight", out var weight));

        Assert.Equal(9f, x);
        Assert.Equal("after", label);
        Assert.Equal(4, speed);
        Assert.Equal(2.5f, weight);
        Assert.False(accessors.TrySet(component, "Position.x", "wrong"));
    }

    [Fact]
    public void AccessorTree_does_not_reuse_stale_script_type_after_reload()
    {
        using var old = LoadScript("public sealed class Reloaded { public int Value; }");
        using var current = LoadScript("public sealed class Reloaded { public string Name = \"new\"; }");
        var oldType = old.Assembly.GetType("Reloaded")!;
        var currentType = current.Assembly.GetType("Reloaded")!;
        var oldInstance = Activator.CreateInstance(oldType)!;
        var currentInstance = Activator.CreateInstance(currentType)!;
        var oldAccessors = ComponentAccessorTree.Create(oldType);

        Assert.True(oldAccessors.TrySet(oldInstance, "Value", 12));
        Assert.False(oldAccessors.TryGet(currentInstance, "Value", out _));
    }

    [Fact]
    public void Collectible_script_context_is_released_after_compiler_and_accessor_release()
    {
        WeakReference context = CreateCollectibleScriptContext();

        ForceGc();

        Assert.False(context.IsAlive);
    }

    private static LoadedScript LoadScript(string source)
    {
        var compiler = new RoslynScriptCompiler();
        var result = compiler.Compile(new ScriptCompilationRequest(
            [new ScriptSource("Reloaded.cs", source)],
            RuntimeReferences(),
            "UserScripts"));
        Assert.True(result.Success);

        var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
        using var stream = new MemoryStream(result.AssemblyBytes.ToArray(), writable: false);
        return new LoadedScript(context, context.LoadFromStream(stream));
    }

    private static WeakReference CreateCollectibleScriptContext()
    {
        var compiler = new RoslynScriptCompiler();
        var result = compiler.Compile(new ScriptCompilationRequest(
            [new ScriptSource("Collectible.cs", "public sealed class Collectible { public int Value; }")],
            RuntimeReferences(),
            "CollectibleScripts"));
        Assert.True(result.Success);

        var context = new AssemblyLoadContext(Guid.NewGuid().ToString(), isCollectible: true);
        using var stream = new MemoryStream(result.AssemblyBytes.ToArray(), writable: false);
        var assembly = context.LoadFromStream(stream);
        var type = assembly.GetType("Collectible")!;
        var instance = Activator.CreateInstance(type)!;
        var accessors = ComponentAccessorTree.Create(type);
        Assert.True(accessors.TrySet(instance, "Value", 7));

        var weakContext = new WeakReference(context);
        context.Unload();
        return weakContext;
    }

    private static void ForceGc()
    {
        for (int i = 0; i < 3; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
        }
    }

    private static IReadOnlyList<IScriptReference> RuntimeReferences()
    {
        var paths = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(float3).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return paths.Select(path => (IScriptReference)new FileScriptReference(path)).ToArray();
    }

    private sealed record LoadedScript(AssemblyLoadContext Context, Assembly Assembly) : IDisposable
    {
        public void Dispose() => Context.Unload();
    }

    [AttributeUsage(AttributeTargets.Field)]
    private sealed class EditableAttribute : Attribute;

#pragma warning disable CS0169
    public sealed class TransformComponent
    {
        public float3 Position;
        public string Label = string.Empty;
        public Nested Nested;
        [Editable]
        private int _speed;
    }

    public struct Nested
    {
        [Editable]
        private float _weight;
    }
#pragma warning restore CS0169
}
