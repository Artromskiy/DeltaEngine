using Delta.Assets;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ConsoleModelImporter
{
    internal class Program
    {
        private static readonly JsonSerializerOptions _options = new(JsonSerializerOptions.Default)
        {
            IgnoreReadOnlyFields = false,
            IncludeFields = true,
            IgnoreReadOnlyProperties = true,
            AllowTrailingCommas = false,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
            TypeInfoResolver = new DefaultJsonTypeInfoResolver
            {
                Modifiers = { AddPrivateFieldsModifier }
            },
        };
        private static void Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            if (args.Length == 0)
                return; // return if no file was dragged onto exe
            var fbxPath = args[0];
            var meshes = ModelImporter.ImportAndGet(fbxPath);
            foreach (var (meshData, name) in meshes)
            {
                string path = Path.GetDirectoryName(args[0])
                   + Path.DirectorySeparatorChar
                   + Path.GetFileNameWithoutExtension(args[0])
                   + "_" + name + "mesh";
                path = CreateIndexedFile(path);
                Serialize(path, meshData);
            }
        }


        /// <summary>
        /// Return path to new file with <paramref name="fullPath"/> name if it does not exist
        /// or adds indexer to the end of name and increments it till file does not exist
        /// </summary>
        /// <param name="fullPath"></param>
        /// <returns></returns>
        private static string CreateIndexedFile(string fullPath)
        {
            if (!File.Exists(fullPath))
                return fullPath;

            string alternateFilename;
            int fileNameIndex = 1;
            var filenameSpan = fullPath.AsSpan();
            var directory = Path.GetDirectoryName(filenameSpan);
            var plainName = Path.GetFileNameWithoutExtension(filenameSpan);
            var extension = Path.GetExtension(filenameSpan);

            StringBuilder sb = new();
            do
                sb.Clear().
                Append(directory).
                Append(Path.DirectorySeparatorChar).
                Append(plainName).
                Append("_").
                Append(fileNameIndex++).
                Append(extension);
            while (File.Exists(alternateFilename = sb.ToString()));

            return alternateFilename;
        }
        private static void AddPrivateFieldsModifier(JsonTypeInfo jsonTypeInfo)
        {
            if (jsonTypeInfo.Kind != JsonTypeInfoKind.Object)
                return;

            foreach (FieldInfo field in jsonTypeInfo.Type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                JsonPropertyInfo jsonPropertyInfo = jsonTypeInfo.CreateJsonPropertyInfo(field.FieldType, field.Name);
                jsonPropertyInfo.Get = field.GetValue;
                jsonPropertyInfo.Set = field.SetValue;

                jsonTypeInfo.Properties.Add(jsonPropertyInfo);
            }
        }

        public static void Serialize<T>(string path, T value)
        {
            using FileStream stream = File.Create(path);
            JsonSerializer.Serialize(stream, value, _options);
        }
    }
}
