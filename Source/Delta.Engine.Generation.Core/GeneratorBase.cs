using Microsoft.CodeAnalysis;
using System;
using System.Diagnostics;

namespace Delta.Engine.Generation.Core;

public abstract class GeneratorBase : IIncrementalGenerator
{
    void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
    {
        try
        {
            Generate(context);
        }
        catch(Exception e)
        {
            Debug.Assert(false, e.Message);
        }
    }
    public abstract void Generate(IncrementalGeneratorInitializationContext context);
}
