using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.Llm;

public static class LlmServiceCollectionExtensions
{
    public static IServiceCollection AddLlmFeature(this IServiceCollection services)
    {
        _ = services.AddSingleton<ILlmService, LlmService>();
        _ = services.AddScoped<LlmCommitMessageGenerator>();
        return services;
    }
}
