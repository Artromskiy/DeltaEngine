using Delta.Runtime;

namespace DeltaEditorLib.Loader;

public class ProjectCreator
{
    private readonly IProjectPath _projectPath;
    public ProjectCreator(IProjectPath projectPath)
    {
        _projectPath = projectPath;
        SetupProjectDirectory();
    }

    private void SetupProjectDirectory()
    {
        Directory.CreateDirectory(_projectPath.AssetsDirectory);
        Directory.CreateDirectory(_projectPath.ScriptsDirectory);
        Directory.CreateDirectory(_projectPath.ResourcesDirectory);
        Directory.CreateDirectory(_projectPath.ProjectDirectory);
        Directory.CreateDirectory(_projectPath.DllDirectory);

        File.Create(_projectPath.ScenesFile).Dispose();
        File.Create(_projectPath.SettingsFile).Dispose();
    }
}
