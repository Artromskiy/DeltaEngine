namespace DeltaEditorLib.Project
{
    public sealed class ProjectPath(string path)
    {
        public readonly string path = path;

        private const string PathsFolder = "Project";

        private const string ProjectSettings = "Settings";
        private const string ProjectScenes = "Scenes";

        public readonly string ProjectSettingsPath = Path.Combine(path, PathsFolder, ProjectSettings);
        public readonly string ProjectScenesPath = Path.Combine(path, PathsFolder, ProjectScenes);
    }
}
