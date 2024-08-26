namespace DeltaEditor.Explorer
{
    internal class ExplorerFileView : ContentView
    {
        private readonly VerticalStackLayout _grid = [];
        private readonly Label _name = new();
        private readonly Image _image = new() { Source = "folder_with_files_svgrepo_com.png" };

        private string? _filePath;

        public event Action<ExplorerFileView>? OnClicked;
        public event Action<ExplorerFileView>? OnDoubleClicked;
        private void OnTapped(object? sender, TappedEventArgs eventArgs) => OnClicked?.Invoke(this);
        private void OnDoubleTapped(object? sender, TappedEventArgs eventArgs) => OnDoubleClicked?.Invoke(this);

        public string FilePath
        {
            get => _filePath!;
            set
            {
                _filePath = value;
                _name.Text = Path.GetFileNameWithoutExtension(value);
            }
        }

        public ExplorerFileView(string path)
        {
            _image.Aspect = Aspect.AspectFit;
            _grid.Add(_name);
            _grid.Add(_image);
            FilePath = path;

            var gesture = new TapGestureRecognizer();
            var doubleTapGesture = new TapGestureRecognizer() { NumberOfTapsRequired = 2 };
            gesture.Tapped += OnTapped;
            doubleTapGesture.Tapped += OnDoubleTapped;
            GestureRecognizers.Add(gesture);
            GestureRecognizers.Add(doubleTapGesture);
            Content = _grid;
        }

        public bool Selected
        {
            set
            {
                _image.Source = value ? "folder_check_svgrepo_com.png" : "folder_with_files_svgrepo_com.png";
            }
        }
    }
}
