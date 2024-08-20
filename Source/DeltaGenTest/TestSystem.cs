using Delta;
using Delta.Files;

namespace DeltaGenTest;

[System]
internal partial record struct TestSystemClass
{
    [System]
    internal partial struct InnerTest
    {
        [SystemCall]
        private void Some(int a, string b)
        {
            
        }

        internal partial struct InnerTest2
        {
            [System]
            internal partial struct InnerTest
            {
                [SystemCall]
                private void Some(int a, string b)
                {

                }
            }
        }
    }




    [SystemCall]
    private void DoOne(ref int k)
    {
        //Update
    }

    [SystemCall]
    private static void DoZeroStatic()
    {
    }

    [SystemCall]
    private static void DoZero()
    {
    }


    [SystemCall]
    private void DoTwo(ref int k, ref byte l)
    {
    }

    [SystemCall]
    private void DoTwo(ref byte k, ref byte l)
    {
    }

    [SystemCall]
    private void DoTwoSame(ref int k, ref int l)
    {
    }

    [SystemCall]
    private void DoGeneric(ref GuidAsset<MeshData> guidAsset)
    {
    }

    [SystemCall]
    private void DoVariousMods(ref int a, in int b, ref readonly int c, int d)
    {
    }

    [SystemCall]
    private void DoVariousMods(ref int a, in int b, ref readonly int c, GuidAsset<MeshData> m)
    {
    }


    [SystemCall]
    private void DoObject(ref object o)
    {
    }
}