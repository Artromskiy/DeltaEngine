using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.Json;
using Delta.Engine.Integration;
using Delta.Engine.Windowed;
using Delta.Render.Core;
using Xunit;

namespace Delta.Engine.Windowed.Tests;

public sealed class WindowedCompositionTests
{
    [Fact]
    public void Sdf_uniforms_use_delta_maths_and_host_elapsed_time()
    {
        var uniforms = FullscreenSdfShaderFixture.CreateUniforms(new EngineSurfaceSnapshot(800, 600), 1.25f);

        Assert.Equal(800, uniforms.Resolution.x);
        Assert.Equal(600, uniforms.Resolution.y);
        Assert.Equal(1.25f, uniforms.TimeSeconds);
        Assert.Contains(nameof(IRenderWindowFrameSession.DrawFullscreenTriangle),
            typeof(IRenderWindowFrameSession).GetMethods().Select(static method => method.Name));
    }

    [Fact]
    public void Host_orders_platform_poll_world_render_and_ui()
    {
        var calls = new List<string>();
        var input = new FakeInput(calls);
        var world = new FakeWorld(calls);
        var render = new FakeRender(calls);
        var ui = new FakeUi(calls);
        using var host = new EngineHost(input, world, render, ui);

        host.Start();
        host.RunFrame(0.5f);

        Assert.Equal(new[] { "input.init", "world.init", "render.init", "ui.init", "input.poll", "world.update", "render.frame", "ui.update" }, calls);
        Assert.Equal(0.5f, render.ElapsedSeconds);
        Assert.Equal(new EngineSurfaceSnapshot(320, 200), render.Surface);
    }

    [Fact]
    public void Renderer_has_no_input_polling_hook()
    {
        var renderMethods = typeof(IEngineRenderService).GetMethods().Select(static method => method.Name).ToArray();

        Assert.DoesNotContain(nameof(IEngineInputService.PollInput), renderMethods);
    }

    [Fact]
    public void Ui_provider_selects_generated_ui_pair_with_matching_push_constant_metadata()
    {
        var fullscreen = WindowShaderArtifactSelection.For(false);
        var ui = WindowShaderArtifactSelection.For(true);

        Assert.Equal("fullscreen-rounded-rectangle.vert", fullscreen.VertexName);
        Assert.Equal("fullscreen-rounded-rectangle.frag", fullscreen.FragmentName);
        Assert.Equal("ui-panel.vert", ui.VertexName);
        Assert.Equal("ui-panel.frag", ui.FragmentName);
        Assert.True(ui.UsesUiPushConstants);
        Assert.False(fullscreen.UsesUiPushConstants);

        var fullscreenVertex = ReadManifest(fullscreen.VertexName);
        var fullscreenFragment = ReadManifest(fullscreen.FragmentName);
        var uiVertex = ReadManifest(ui.VertexName);
        var uiFragment = ReadManifest(ui.FragmentName);

        Assert.Equal(Delta.Shader.Abstractions.ShaderStage.Vertex, uiVertex.Stage);
        Assert.Equal(Delta.Shader.Abstractions.ShaderStage.Fragment, uiFragment.Stage);
        Assert.Equal(uiVertex.PushConstants[0].Size, uiFragment.PushConstants[0].Size);
        Assert.True(fullscreenVertex.PushConstants.Count == 0);
        Assert.NotEqual(fullscreenFragment.PushConstants[0].Size, uiFragment.PushConstants[0].Size);
    }

    private static Delta.Shader.Abstractions.ShaderAbiManifest ReadManifest(string shaderName)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "shaders", shaderName + ".shader.json");
        return JsonSerializer.Deserialize<Delta.Shader.Abstractions.ShaderAbiManifest>(File.ReadAllText(path))
            ?? throw new InvalidDataException(path);
    }

    private sealed class FakeInput(List<string> calls) : IEnginePlatformShell
    {
        public EngineSurfaceSnapshot Surface => new(320, 200);
        public void Initialize() => calls.Add("input.init");
        public InputSnapshot PollInput(int frameNumber, float deltaSeconds)
        {
            calls.Add("input.poll");
            return new InputSnapshot(frameNumber, Surface: Surface);
        }
        public void Shutdown() { }
        public void Dispose() { }
    }

    private sealed class FakeWorld(List<string> calls) : IEngineWorldService
    {
        public void Initialize() => calls.Add("world.init");
        public void Update(in EngineFrameContext context) => calls.Add("world.update");
        public void Shutdown() { }
        public void Dispose() { }
    }

    private sealed class FakeRender(List<string> calls) : IEngineRenderService
    {
        public float ElapsedSeconds { get; private set; }
        public EngineSurfaceSnapshot Surface { get; private set; }
        public void Initialize() => calls.Add("render.init");
        public void Render(in EngineFrameContext context)
        {
            calls.Add("render.frame");
            ElapsedSeconds = context.ElapsedSeconds;
            Surface = context.Surface;
        }
        public void Shutdown() { }
        public void Dispose() { }
    }

    private sealed class FakeUi(List<string> calls) : IEngineUiService
    {
        public void Initialize() => calls.Add("ui.init");
        public void Update(in EngineFrameContext context) => calls.Add("ui.update");
        public void Shutdown() { }
        public void Dispose() { }
    }
}
