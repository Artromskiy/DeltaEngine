using CommunityToolkit.Maui;
using Delta.Runtime;
using DeltaEditorLib.Loader;
using Microsoft.Extensions.Logging;

namespace DeltaEditor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        string directoryPath  = ProjectCreator.GetExecutableDirectory();
        var projectPath = new EditorPaths(directoryPath);
        ProjectCreator.CreateProject(projectPath);

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .Services
            .AddSingleton<IUIThreadGetter>(new MauiThreadGetter())
            .AddSingleton<IProjectPath>(projectPath)
            .AddSingleton<RuntimeLoader>()
            .AddSingleton<MainPage>();
#if DEBUG
        builder.Logging.AddDebug();
        System.Diagnostics.Debugger.Launch();
#endif
        MauiApp app = builder.Build();


        return app;
    }
}