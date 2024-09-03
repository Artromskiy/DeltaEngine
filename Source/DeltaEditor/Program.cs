using Avalonia;
using Delta.Runtime;
using DeltaEditorLib.Loader;
using System;
using System.IO;

namespace DeltaEditor;

internal class Program
{
    public static IProjectPath ProjectPath { get; private set; }
    public static RuntimeLoader RuntimeLoader { get; private set; }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        string directoryPath = ProjectCreator.GetExecutableDirectory();
        ProjectPath = new EditorPaths(directoryPath);
        ProjectCreator.CreateProject(ProjectPath);
        IUIThreadGetter uiThreadGetter = new AvaloniaThreadGetter();
        RuntimeLoader = new RuntimeLoader(ProjectPath, uiThreadGetter);

        BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
    {
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.SvgImageExtension).Assembly);
        GC.KeepAlive(typeof(Avalonia.Svg.Skia.Svg).Assembly);

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}
