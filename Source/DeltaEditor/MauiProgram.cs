using Delta.Runtime;
using DeltaEditorLib.Scripting;
using Microsoft.Extensions.Logging;

namespace DeltaEditor
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            string projectPath;
            var arguments = Environment.GetCommandLineArgs();
            foreach (var item in arguments)
                Console.WriteLine(item);

            bool projectExist = arguments.Length > 1 && Directory.Exists(arguments[1]);

            if (projectExist)
                projectPath = arguments[1];
            else
                projectPath = Directory.CreateTempSubdirectory().FullName;

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
            var builded = builder.Build();

            if(!projectExist)
                new ProjectCreator(builded.Services.GetService<RuntimeLoader>()!).FullSetup();

            return builded;
        }
    }
}
