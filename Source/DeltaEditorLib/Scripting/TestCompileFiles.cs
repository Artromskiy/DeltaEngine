using System.Text;

namespace DeltaEditorLib.Scripting
{
    internal class TestCompileFiles
    {
        private const string TestCsFileName = "Something";
        private const string TestCsFile =
            """
            using System;
            using Delta;
            using Delta.Scripting;
            using System.Diagnostics;

            [Component]
            public class Something
            {
                public void Do()
                {
                    Debug.WriteLine("Compile World!");
                }
            }
            """
            ;

        public static void CreateTestFile(string path)
        {
            string fullPath = Path.Combine(path, TestCsFileName) + ".cs";
            File.WriteAllText(fullPath, TestCsFile, Encoding.UTF8);
        }
    }
}
