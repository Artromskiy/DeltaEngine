using System;
using Delta.Engine.Integration;
using Delta.Render.Core;
using Delta.Render.Platform.SDL3;
using Delta.Render.Vulkan;

namespace Delta.Engine.Windowed;

internal static class Program
{
    private static int Main(string[] args)
    {
        var frameLimit = ParseFrameLimit(args);
        var platform = new Sdl3PlatformShell(
            new Sdl3WindowFactory(),
            new WindowConfiguration("Delta Engine SDF", 960, 540, true, true));
        var renderer = new VulkanRenderer(new VulkanRendererOptions());
        var renderService = new VulkanWindowRenderService(platform, renderer);
        var host = new EngineHost(platform, new WindowedNoopWorld(), renderService, new WindowedNoopUi());

        try
        {
            using (host)
            {
                host.Start();
                var clock = new Sdl3FrameClock();
                while (host.IsRunning && (frameLimit is null || host.CompletedFrames < frameLimit.Value))
                {
                    host.RunFrame(clock.NextDeltaSeconds());
                }

                return 0;
            }
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine(exception);
            return 1;
        }
    }

    private static int? ParseFrameLimit(string[] args)
    {
        for (var index = 0; index + 1 < args.Length; index++)
        {
            if (string.Equals(args[index], "--frames", StringComparison.OrdinalIgnoreCase) && int.TryParse(args[index + 1], out var value))
            {
                return value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(args), "--frames must be non-negative.");
            }
        }

        return null;
    }
}
