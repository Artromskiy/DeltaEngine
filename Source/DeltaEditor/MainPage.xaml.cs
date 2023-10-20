namespace DeltaEditor
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            MauiProgram._engine.Run();
            var f = MainGrid[0].Frame;
            MauiProgram._engine.SetWindowPositionAndSize(((int)f.X, (int)f.Y, (int)f.Width, (int)f.Height));
        }

        protected override void OnSizeAllocated(double width, double height)
        {
            base.OnSizeAllocated(width, height);
            var f = MainGrid[0].Frame;
            //var pos = GetAbsolutePosition(MainGrid[1]);
            MauiProgram._engine.SetWindowPositionAndSize(((int)f.X, (int)f.Y, (int)f.Width, (int)f.Height));
        }
    }
}