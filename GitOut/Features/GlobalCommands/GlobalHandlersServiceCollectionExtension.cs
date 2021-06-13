using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.GlobalCommands
{
    public static class GlobalHandlersServiceCollectionExtension
    {
        public static void AddGlobalHandlersFeature(this IServiceCollection services)
        {
            services.AddScoped<IGlobalCommandHandler, OpenHandler>();
            services.AddScoped<IGlobalCommandHandler, CopyHandler>();
            services.AddScoped<IGlobalCommandHandler, RevealInExplorerHandler>();
        }
    }
}
