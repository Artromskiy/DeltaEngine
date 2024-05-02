using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;

namespace DeltaEditorLib.Compile
{
    internal static class VariadicsGenerator
    {
        public static string GenerateVariadics(int count)
        {
            var refTuples = "RefTuple";
            var readonlyRefTuples = "ReadonlyRefTuple";
            var byRef = GenerateVariadicContainer(refTuples, count, ReadonlyParam.No, ByRefParam.Yes);
            var read = GenerateVariadicContainer(readonlyRefTuples, count, ReadonlyParam.Yes, ByRefParam.Yes);
            return byRef;
        }

        private static string GenerateVariadicContainer(string name, int count, ReadonlyParam readonlyParam, ByRefParam byRefParam)
        {
            StringBuilder code = new();
            for (int i = 1; i < count + 1; i++)
            {
                code.Append($"public {readonlyParam.String()}{byRefParam.String()} struct {name}");
                code.AppendGenericArgs(i);
                code.Append('{').AppendLine();
                code.AppendGenericFields(i, readonlyParam, byRefParam);
                code.Append('}').AppendLine();
            }
            return CSharpSyntaxTree.ParseText(code.ToString()).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
        }

        private static void AppendGenericFields(this StringBuilder code, int count, ReadonlyParam readonlyParam, ByRefParam byRefParam)
        {
            for (int j = 0; j < count; j++)
                code.Append($"public {readonlyParam.String()}{byRefParam.String()}T{j} Item{j};").AppendLine();
        }

        private static string String(this ReadonlyParam param)
        {
            const string readonlyParam = "readonly ";
            return param == ReadonlyParam.Yes ? readonlyParam : string.Empty;
        }

        private static string String(this ByRefParam param)
        {
            const string byRefParam = "ref ";
            return param == ByRefParam.Yes ? byRefParam : string.Empty;
        }

        private enum ByRefParam
        {
            No,
            Yes
        }

        private enum ReadonlyParam
        {
            No,
            Yes
        }

        private static void AppendGenericArgs(this StringBuilder code, int count)
        {
            code.Append('<');
            for (int j = 0; j < count; j++)
            {
                code.Append('T').Append(j);
                if (j != count - 1)
                    code.Append(", ");
            }
            code.Append('>');
        }
    }
}
