using Delta.Runtime;
using DeltaEditorLib.Loader;
using Microsoft.Maui.Layouts;

namespace DeltaEditor.Explorer
{
    internal class ExplorerView : ContentView
    {
        private readonly FlexLayout _grid = [];
        private string? _currentPath;

        private ExplorerFileView? _selectedExplorerFileView;

        private bool isDirty = true;

        public ExplorerView(RuntimeLoader runtimeLoader)
        {
            _grid.Wrap = FlexWrap.Wrap;
            _grid.JustifyContent = FlexJustify.SpaceAround;
            Content = _grid;
        }

        public void UpdateExplorer(IRuntime runtime)
        {
            if (!isDirty)
                return;

            _currentPath ??= runtime.Context.ProjectPath.RootDirectory;
            _grid.Clear();
            var directories = Directory.EnumerateFileSystemEntries(_currentPath);
            foreach (var path in directories)
            {
                var explorerView = new ExplorerFileView(path);
                explorerView.MaximumHeightRequest = 100;
                explorerView.MaximumWidthRequest = 100;
                explorerView.OnClicked += SelectFile;
                explorerView.OnDoubleClicked += OpenFile;
                _grid.Add(explorerView);
            }
            isDirty = false;
        }

        private void SelectFile(ExplorerFileView explorerFileView)
        {
            if (_selectedExplorerFileView != null)
                _selectedExplorerFileView.Selected = false;
            _selectedExplorerFileView = explorerFileView;
            _selectedExplorerFileView.Selected = true;
        }

        private void OpenFile(ExplorerFileView explorerFileView)
        {
            if (!Directory.Exists(explorerFileView.FilePath))
                return;
            _currentPath = explorerFileView.FilePath;
            _selectedExplorerFileView = null;
            isDirty = true;
        }
    }
}
