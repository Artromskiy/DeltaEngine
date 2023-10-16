using Silk.NET.SDL;
using Silk.NET.Vulkan;
using System;

namespace DeltaEngine
{
    internal static unsafe class ThrowHelper
    {
        public static ResultStruct _
        {
            set
            {
                if (value.notSucceed)
                    throw new NotSucceedException();
            }
        }


        internal class NotSucceedException : Exception
        {
            public override string Message => base.Message;
        }

        internal readonly struct ResultStruct
        {
            public readonly bool notSucceed;

            private ResultStruct(bool succeed)
            {
                notSucceed = !succeed;
            }
            public ResultStruct() : this(false) { }

            public static implicit operator ResultStruct(Result r)=>new (r == Result.Success);
            public static implicit operator ResultStruct(SdlBool r) => new(r == SdlBool.True);
            public static implicit operator ResultStruct(bool r) => new(r);
            public static implicit operator ResultStruct(int r) => new(r == 0);
        }
    }
}
