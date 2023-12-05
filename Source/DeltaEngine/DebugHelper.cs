using Silk.NET.SDL;
using Silk.NET.Vulkan;

namespace DeltaEngine;

/// <summary>
/// Helps debuggins condition results
/// Currently supports implicit casts of
/// Bool, SdlBool, Vulkan.Result.
/// 
/// Should be used as static directive eg:
/// using static DeltaEngine.DebugHelper
/// </summary>
internal static unsafe class DebugHelper
{

#if ASSERT
    /// <summary>
    /// Discard symbol used to simplify debug
    /// It will do nothing if assignment is going under release mode
    /// Currently supports implicit casts of
    /// Bool, SdlBool, Vulkan.Result.
    /// </summary>
    internal static ResultStruct _
    {
        set => Debug.Assert(value);
    }
#endif

    /// <summary>
    /// Struct used for implicit casts of various types for debugging
    /// Ref added to prevent boxing by user
    /// </summary>
    internal readonly ref struct ResultStruct
    {
        internal readonly bool succeed;
        internal readonly string data;

        private ResultStruct(bool succeed, string data)
        {
            this.data = data;
            this.succeed = succeed;
        }

        /// <summary>
        /// Default initializer shouldn't be used directly
        /// So by default it's result is false
        /// </summary>
        public ResultStruct() : this(false, string.Empty) { }

        public static implicit operator bool(ResultStruct s) => s.succeed;

        public static implicit operator ResultStruct(Result r) => new(r == Result.Success, r.ToString());
        public static implicit operator ResultStruct(SdlBool r) => new(r == SdlBool.True, string.Empty);
        public static implicit operator ResultStruct(bool r) => new(r, string.Empty);
    }
}
