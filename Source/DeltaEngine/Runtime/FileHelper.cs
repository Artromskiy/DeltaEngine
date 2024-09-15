using System;
using System.IO;
using System.Text;

namespace Delta.Runtime;
internal static class FileHelper
{
    private const char Underscore = '_';

    /// <summary>
    /// Return path to new file with <paramref name="fullPath"/> name if it does not exist
    /// or adds indexer to the end of name and increments it till file does not exist
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns></returns>
    public static string CreateIndexedFile(string fullPath)
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
            Append(Underscore).
            Append(fileNameIndex++).
            Append(extension);
        while (File.Exists(alternateFilename = sb.ToString()));

        return alternateFilename;
    }

    /// <summary>
    /// Return path to new file in <paramref name="folder"/> with <paramref name="fileName"/> name if it does not exist
    /// or adds indexer to the end of name and increments it till file does not exist
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns></returns>
    public static string CreateIndexedFile(string folder, string fileName)
    {
        string fullPath = Path.Combine(folder, fileName);
        return CreateIndexedFile(fullPath);
    }

    /// <summary>
    /// Return path to new file in <paramref name="folder"/> with <paramref name="guid"/> name
    /// and <paramref name="extension"/> if it does not exist
    /// or adds indexer to the end of name and increments it till file does not exist
    /// </summary>
    /// <param name="fullPath"></param>
    /// <returns></returns>
    public static string CreateIndexedFile(string folder, Guid guid, string extension)
    {
        string fullPath = Path.ChangeExtension(Path.Combine(folder, guid.ToString()), extension);
        return CreateIndexedFile(fullPath);
    }
}
