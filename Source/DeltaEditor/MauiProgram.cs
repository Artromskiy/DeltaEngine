using Delta.Runtime;
using DeltaEditorLib.Loader;
using System.Globalization;
using UraniumUI;

namespace DeltaEditor;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        bool projectExist = arguments.Length > 1 && Directory.Exists(arguments[1]);
        string projectPath = projectExist ? arguments[1] : Directory.CreateTempSubdirectory().FullName;
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseUraniumUI()
            .UseUraniumUIMaterial()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            })
            .Services
            .AddSingleton<IUIThreadGetter>(new MauiThreadGetter())
            .AddSingleton<IProjectPath>(new EditorPaths(projectPath))
            .AddSingleton<RuntimeLoader>()
            .AddSingleton<MainPage>();
#if DEBUG
        builder.Logging.AddDebug();
        System.Diagnostics.Debugger.Launch();
#endif
        MauiApp app = builder.Build();

        if (!projectExist)
            new ProjectCreator(app.Services.GetService<RuntimeLoader>()!, app.Services.GetService<IProjectPath>()!).FullSetup();

        return app;
    }
}