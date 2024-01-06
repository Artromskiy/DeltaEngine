using CommunityToolkit.Maui.Storage;
using System.Diagnostics;
using System.Text.Json;

namespace DeltaHub
{
    public partial class MainPage : ContentPage
    {
        private const string editorAppName = "DeltaEditor.exe";
        private const string EditorPathsFileName = "EditorPaths.json";

        private readonly string _savedEditorPathsFile;
        private readonly string _appDataFolder;

        private readonly Dictionary<string, Process> _projectToProcess = [];
        private readonly HashSet<string> _savedEditorPaths = [];

        public MainPage()
        {
            InitializeComponent();
            _appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppInfo.Name);
            _savedEditorPathsFile = Path.Combine(_appDataFolder, EditorPathsFileName);
            Directory.CreateDirectory(_appDataFolder);
            LoadEditorPaths();
        }

        private async void OnCreateProjectClicked(object sender, EventArgs e)
        {
            var folderPick = await FolderPicker.Default.PickAsync();

            if (_savedEditorPaths.Count == 0 || !folderPick.IsSuccessful)
                return;

            var path = folderPick.Folder.Path;
            if (!IsDirectoryEmpty(path) || _projectToProcess.ContainsKey(path))
                return;

            var editorPath = _savedEditorPaths.First();
            ProcessStartInfo startInfo = new(editorPath, [path])
            {
                WorkingDirectory = Path.GetDirectoryName(editorPath),
                UseShellExecute = false,
                ErrorDialog = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
            };
            try
            {
                Process exeProcess = Process.Start(startInfo);
                if (exeProcess != null)
                {
                    _projectToProcess.Add(path, exeProcess);
                    exeProcess.Exited += (o, h) => _projectToProcess.Remove(path);
                    if (exeProcess.HasExited)
                        _projectToProcess.Remove(path);
                }
            }
            catch (Exception)
            {

            }
        }

        private async void OnSelectEditorFolder(object sender, EventArgs e)
        {
            var folderPick = await FolderPicker.Default.PickAsync();
            if (folderPick.IsSuccessful)
                SelectEditorPaths(folderPick.Folder.Path);
        }

        private void SelectEditorPaths(string folderPath)
        {
            _savedEditorPaths.Clear();
            var exes = Directory.EnumerateFiles(folderPath, "*.exe", SearchOption.AllDirectories);
            foreach (var path in exes)
            {
                var filename = Path.GetFileName(path.AsSpan());
                if (filename.Equals(editorAppName, StringComparison.Ordinal))
                    _savedEditorPaths.Add(path);
            }
            SaveEditorPaths();
        }

        private void SaveEditorPaths()
        {
            using Stream stream = File.Create(_savedEditorPathsFile);
            JsonSerializer.Serialize(stream, _savedEditorPaths);
        }

        private void LoadEditorPaths()
        {
            if (File.Exists(_savedEditorPathsFile))
            {
                using Stream stream = new FileStream(_savedEditorPathsFile, FileMode.Open, FileAccess.Read);
                var deserializedSet = JsonSerializer.Deserialize<HashSet<string>>(stream);
                if (deserializedSet != null)
                {
                    _savedEditorPaths.Clear();
                    _savedEditorPaths.UnionWith(deserializedSet);
                }
            }
        }

        private static bool IsDirectoryEmpty(string path)
        {
            IEnumerable<string> items = Directory.EnumerateFileSystemEntries(path);
            using IEnumerator<string> en = items.GetEnumerator();
            return !en.MoveNext();
        }
    }
}