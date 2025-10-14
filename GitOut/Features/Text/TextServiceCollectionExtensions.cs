using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Text;

public static class TextServiceCollectionExtensions
{
    public static IServiceCollection AddTextPromptFeature(this IServiceCollection services) =>
        services.AddTransient<TextPromptPage>().AddTransient<TextPromptViewModel>();
}
