using Silk.NET.Core.Native;
using Silk.NET.Vulkan;
using Silk.NET.Vulkan.Extensions.KHR;
using System.Linq;
using System.Runtime.InteropServices;

namespace DeltaEngine
{
    internal static unsafe class VKHelper
    {

        internal static ExtensionProperties[] GetInstanceExtensions(Vk api)
        {
            uint count = 0;
            api.EnumerateInstanceExtensionProperties((byte*)null, &count, null);
            ExtensionProperties[] result = new ExtensionProperties[(int)count];
            api.EnumerateInstanceExtensionProperties((byte*)null, &count, result);
            return result;
        }

        static VKHelper()
        {
            RequiredExtensions = DefaultExtensions.Concat(WinExtensions).ToArray();
        }

        internal static string[] RequiredExtensions;

        private static readonly string[] DefaultExtensions = new string[]
        {
            KhrSurface.ExtensionName
        };

        private static readonly string[] WinExtensions = new string[]
        {
            KhrWin32Surface.ExtensionName
        };

    }
}
