using Delta.Files;

namespace Delta.Runtime;

internal sealed class ProjectData : IProjectData
{
    private readonly ProjectScenes projectScenes;

    public ProjectData(IProjectPath projectPath)
    {
        projectScenes = Serialization.Deserialize<ProjectScenes>(projectPath.ScenesFile);
    }
}
