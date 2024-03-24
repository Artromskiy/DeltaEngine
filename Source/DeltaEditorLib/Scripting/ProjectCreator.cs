using Delta;
using Delta.Files.Defaults;
using Delta.Runtime;

namespace DeltaEditorLib.Scripting
{
    public class ProjectCreator
    {
        private readonly IRuntime _runtime;
        private readonly IProjectPath _projectPath;
        private readonly IAssetImporter _assetImporter;
        public ProjectCreator(RuntimeLoader loader)
        {
            _runtime = loader.Runtime;
            _projectPath = _runtime.ProjectPath;
            _assetImporter = _runtime.AssetImporter;
            FullSetup();
        }

        public void FullSetup()
        {
            SetupProjectDirectory();
            CreateTestFiles();
        }

        public void CreateTestFiles()
        {
            TestCompileFiles.CreateTestScript(_projectPath.RootDirectory);
            _assetImporter.CreateAsset("deltaMesh", Defaults.Delta);
            _assetImporter.CreateAsset("traingleMesh", Defaults.Triangle);
        }

        private void SetupProjectDirectory()
        {
            Directory.CreateDirectory(_projectPath.AssetsDirectory);
            Directory.CreateDirectory(_projectPath.ScriptsDirectory);
            Directory.CreateDirectory(_projectPath.ResourcesDirectory);
            Directory.CreateDirectory(_projectPath.ProjectDirectory);

            File.Create(_projectPath.ScenesFile).Dispose();
            File.Create(_projectPath.SettingsFile).Dispose();
        }
    }
}
