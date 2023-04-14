using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Text;

public static class TextServiceCollectionExtensions
{
    public static void AddTextPromptFeature(this IServiceCollection services)
    {
        services.AddTransient<TextPromptPage>();
        services.AddTransient<TextPromptViewModel>();
    }
}
