using Delta.Runtime;
using DeltaEditorLib.Scripting;
using Microsoft.Extensions.Logging;

namespace DeltaEditor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool projectExist = arguments.Length > 1 && Directory.Exists(arguments[1]);
            string projectPath = projectExist ? arguments[1] : Directory.CreateTempSubdirectory().FullName;

            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                })
                .Services
                .AddSingleton<IProjectPath>(new EditorPaths(projectPath))
                .AddSingleton<RuntimeLoader>()
                .AddSingleton<MainPage>();
#if DEBUG
            builder.Logging.AddDebug();
            System.Diagnostics.Debugger.Launch();
#endif
            MauiApp app = builder.Build();

            if (!projectExist)
                new ProjectCreator(app.Services.GetService<RuntimeLoader>()!).FullSetup();

            return app;
        }
    }
}