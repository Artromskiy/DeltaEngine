using System;
using System.Linq;
using Delta.Engine.Integration;
using Delta.Editor.Scripting;
using Xunit;

namespace Delta.Engine.Integration.Tests;

public sealed class RuntimeBoundaryContractTests
{
    [Fact]
    public void Integration_runtime_does_not_reference_editor_or_backend_assemblies()
    {
        var references = typeof(EngineHost)
            .Assembly
            .GetReferencedAssemblies()
            .Select(static reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.DoesNotContain(references, static name => name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));
        Assert.DoesNotContain(references, static name => name.StartsWith("Avalonia", StringComparison.Ordinal));
        Assert.DoesNotContain(references, static name => name.StartsWith("Arch", StringComparison.Ordinal));
        Assert.DoesNotContain(references, static name => name.StartsWith("Delta.Engine.Editor", StringComparison.Ordinal));
        Assert.DoesNotContain(references, static name => name.StartsWith("Delta.Render", StringComparison.Ordinal));
        Assert.DoesNotContain(references, static name => name.StartsWith("Delta.Shader", StringComparison.Ordinal));
    }

    [Fact]
    public void Roslyn_compiler_backend_depends_on_neutral_contracts_in_one_direction()
    {
        var references = typeof(RoslynScriptCompiler)
            .Assembly
            .GetReferencedAssemblies()
            .Select(static reference => reference.Name ?? string.Empty)
            .ToArray();

        Assert.Contains("Delta.Engine.Integration", references);
        Assert.Contains(references, static name => name.StartsWith("Microsoft.CodeAnalysis", StringComparison.Ordinal));
        Assert.DoesNotContain(references, static name => name.Equals("Delta.Engine", StringComparison.Ordinal));
    }
}

