using Delta;
using DeltaEditorLib.Project;
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

            if (arguments.Length > 1)
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
                .AddSingleton(new ProjectPath(projectPath))
                .AddSingleton(new Engine(projectPath))
                .AddSingleton<MainPage>();

#if DEBUG
            builder.Logging.AddDebug();
            System.Diagnostics.Debugger.Launch();
#endif
            return builder.Build();
        }
    }
}
