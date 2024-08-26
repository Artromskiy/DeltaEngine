namespace Delta.Runtime;

public interface IProjectPath
{
    public string RootDirectory { get; }

    public string AssetsDirectory { get; }
    public string ScriptsDirectory { get; }
    public string ResourcesDirectory { get; }
    public string DllsDirectory { get; }
    public string ProjectDirectory { get; }

    public string SettingsFile { get; }
    public string ScenesFile { get; }
}
