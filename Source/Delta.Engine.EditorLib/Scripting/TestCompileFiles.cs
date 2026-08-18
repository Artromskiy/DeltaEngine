using System.IO;
using System.Text;
using Delta.Engine.EditorLib.Scripting;

internal class TestCompileFiles
{
    private const string TestCsFileName = "Something";
    private const string TestCsFile =
        """
        using System;
using         Delta.Engine;
using         Delta.Engine.Scripting;
        using System.Diagnostics;

        [Component]
        public class Something
        {
            // Creates 50 MB array
            public static byte[] values = new byte[1024*1024*50];

            public void Do()
            {
                Debug.WriteLine("Compile World!");
            }

            public Something()
            {
                Random.Shared.NextBytes(values);
            }
        }
        """
        ;

    public static void CreateTestScript(string path)
    {
        string fullPath = Path.Combine(path, TestCsFileName) + ".cs";
        File.WriteAllText(fullPath, TestCsFile, Encoding.UTF8);
    }
}
