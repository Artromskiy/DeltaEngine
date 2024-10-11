using Delta.Assets;
using Delta.Runtime;
using Silk.NET.Shaderc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace DeltaEditorLib.Compile;
internal unsafe class ShaderCompilerModule
{
    const string VertexExtension = ".vert";
    const string FragmentExtension = ".frag";
    const string ComputeExtension = ".comp";
    const string GeometryExtension = ".geom";
    const string TessControlExtension = ".tesc";
    const string TessEvaluationExtension = ".tese";

    public void CompileAndImportShaders(string directory)
    {
        //         name               ext     path
        Dictionary<string, Dictionary<string, string>> shaderNameToShaderModules = [];
        foreach (var path in Directory.EnumerateFiles(directory))
        {
            var ext = Path.GetExtension(path);
            var name = Path.GetFileNameWithoutExtension(path);
            if (!shaderNameToShaderModules.TryGetValue(name, out var dict))
                shaderNameToShaderModules[name] = dict = [];
            dict.Add(ext, path);
        }

        foreach (var item in shaderNameToShaderModules)
        {
            if (item.Value.Count != 2)
                continue;
            if (!(item.Value.ContainsKey(VertexExtension) && item.Value.ContainsKey(FragmentExtension)))
                continue;

            var vertBytes = Compile(item.Value[VertexExtension], ShaderKind.VertexShader);
            var fragBytes = Compile(item.Value[FragmentExtension], ShaderKind.FragmentShader);

            //ShaderData shaderData = new(null, null);
            var vertexFlags= SpirvCrossHelper.GetInputAttributes(vertBytes);
            ShaderData shaderData = new(vertBytes, fragBytes, vertexFlags);
            var shaderAsset = IRuntimeContext.Current.AssetImporter.CreateAsset(shaderData, item.Key + ".shader");
            MaterialData materialData = new(shaderAsset);
            IRuntimeContext.Current.AssetImporter.CreateAsset(materialData, item.Key + "Material.mat");
        }
    }

    private static byte[] Compile(string path)
    {
        var extension = Path.GetExtension(path);
        ShaderKind shaderKind = extension switch
        {
            VertexExtension => ShaderKind.VertexShader,
            FragmentExtension => ShaderKind.FragmentShader,
            ComputeExtension => ShaderKind.ComputeShader,
            GeometryExtension => ShaderKind.GeometryShader,
            TessControlExtension => ShaderKind.TessControlShader,
            TessEvaluationExtension => ShaderKind.TessEvaluationShader,
            _ => throw new ArgumentException(null, nameof(path))
        };
        return Compile(path, shaderKind);
    }
    private static byte[] Compile(string path, ShaderKind shaderKind)
    {
        using var api = Shaderc.GetApi();
        var opts = api.CompileOptionsInitialize();
        api.CompileOptionsSetOptimizationLevel(opts, OptimizationLevel.Performance);
        Compiler* compiler = api.CompilerInitialize();
        var fileName = Path.GetFileNameWithoutExtension(path);
        var sourceBytes = File.ReadAllBytes(path);
        CompilationResult* result = api.CompileIntoSpv(compiler, sourceBytes, (nuint)sourceBytes.Length, shaderKind, fileName, "main", opts);
        var status = api.ResultGetCompilationStatus(result);
        Debug.Assert(status == CompilationStatus.Success, status.ToString(), api.ResultGetErrorMessageS(result));
        var length = api.ResultGetLength(result);
        byte[] spv = new Span<byte>(api.ResultGetBytes(result), (int)length).ToArray();
        api.ResultRelease(result);
        api.CompilerRelease(compiler);
        return spv;
    }
}