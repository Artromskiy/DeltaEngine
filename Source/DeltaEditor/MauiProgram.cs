using DeltaEngine;
using Microsoft.Extensions.Logging;

namespace DeltaEditor
{
    public static class MauiProgram
    {
        private static readonly Engine _engine;

        static MauiProgram()
        {
            _engine = new Engine();
            _engine.Run();
        }

        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                });
#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}