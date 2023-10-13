using Microsoft.Extensions.Logging;
using DeltaEngine;

namespace DeltaEngineEditor
{
    public static class MauiProgram
    {
        private static Engine _engine = new();

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