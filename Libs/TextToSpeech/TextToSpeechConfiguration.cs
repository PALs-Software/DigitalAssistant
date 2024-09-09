using Microsoft.Extensions.DependencyInjection;

namespace TextToSpeech;

public class TextToSpeechConfiguration
{
    public string? ModelBaseDirectoryPath { get; set; } = null;
    public string? Model { get; set; } = null;
    public bool UseGpu { get; set; }
    public bool PreventLoadingAiModels { get; set; }
}

public static class TextToSpeechConfigurationServiceCollectionExtension
{
    public static IServiceCollection AddTextToSpeechService(this IServiceCollection serviceCollection, TextToSpeechConfiguration configuration)
    {
        serviceCollection
            .AddSingleton<TextToSpeechService>()
            .AddSingleton(configuration);

        return serviceCollection;
    }
}