using Silk.NET.Vulkan;
using System.Diagnostics;

namespace Delta;

/// <summary>
/// Helps debuggins condition results
/// Currently supports implicit casts of
/// Bool, Vulkan.Result.
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
    /// Bool, Vulkan.Result.
    /// </summary>
    internal static object _
    {
        set
        {
            ResultStruct result = value switch
            {
                Result r => new ResultStruct(r == Result.Success, r.ToString()),
                bool r => new ResultStruct(r),
                _ => new ResultStruct()
            };
            Debug.Assert(result, result.data);

        }
    }
#endif

    /// <summary>
    /// Struct used for implicit casts of various types for debugging
    /// Ref added to prevent boxing by user
    /// </summary>
    private readonly ref struct ResultStruct
    {
        private readonly bool succeed;
        public readonly string data;

        public ResultStruct(bool succeed, string? data = null)
        {
            this.data = data ?? string.Empty;
            this.succeed = succeed;
        }

        /// <summary>
        /// Default initializer shouldn't be used directly
        /// So by default it's result is false
        /// </summary>
        public ResultStruct() : this(false, string.Empty) { }

        public ResultStruct(Result r) : this(r == Result.Success, r.ToString()) { }
        public ResultStruct(bool r) : this(r, string.Empty) { }
        public ResultStruct(object _) : this() { }

        public static implicit operator bool(ResultStruct s) => s.succeed;
    }
}
