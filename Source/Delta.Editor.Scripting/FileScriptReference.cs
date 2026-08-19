using Delta.Engine.Integration;
using System;
using System.IO;

namespace Delta.Editor.Scripting;

public sealed class FileScriptReference : IScriptReference
{
    private readonly Lazy<byte[]> _image;

    public FileScriptReference(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        Path = System.IO.Path.GetFullPath(path);
        Name = System.IO.Path.GetFileName(Path);
        _image = new Lazy<byte[]>(() => File.ReadAllBytes(Path), isThreadSafe: true);
    }

    public string Path { get; }

    public string Name { get; }

    public Stream OpenRead() => new MemoryStream(_image.Value, writable: false);
}
