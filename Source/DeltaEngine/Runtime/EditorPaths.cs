using System.IO;

namespace Delta.Runtime;

public class EditorPaths(string path) : IProjectPath
{
    private const string Assets = nameof(Assets);

    private const string Project = nameof(Project);
    private const string Resources = nameof(Resources);
    private const string Scripts = nameof(Scripts);

    private const string Settings = nameof(Settings);
    private const string Scenes = nameof(Scenes);

    private const string json = nameof(json);

    public string RootDirectory { get; } = path;

    public string AssetsDirectory { get; } = Path.Combine(path, Assets);
    public string ResourcesDirectory { get; } = Path.Combine(path, Assets, Resources);
    public string ScriptsDirectory { get; } = Path.Combine(path, Assets, Scripts);

    public string ProjectDirectory { get; } = Path.Combine(path, Project);


    public string SettingsFile { get; } = Path.ChangeExtension(Path.Combine(path, Project, Settings), json);
    public string ScenesFile { get; } = Path.ChangeExtension(Path.Combine(path, Project, Scenes), json);
}
