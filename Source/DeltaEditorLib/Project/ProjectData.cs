using Delta.Files;

namespace DeltaEditorLib.Project
{
    public sealed class ProjectData
    {
        private readonly ProjectScenes projectScenes;

        public ProjectData(ProjectPath projectPath)
        {
            projectScenes = Serialization.Deserialize<ProjectScenes>(projectPath.ProjectScenesPath);
        }
    }
}
