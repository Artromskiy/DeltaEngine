using Silk.NET.SDL;
using Silk.NET.Vulkan;

namespace Delta.Rendering.Internal;

internal readonly struct Api
{
    public readonly Sdl sdl;
    public readonly Vk vk;
    public Api()
    {
        sdl = Sdl.GetApi();
        vk = Vk.GetApi();
        sdl.Init(Sdl.InitVideo | Sdl.InitEvents);
    }
}
