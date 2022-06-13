using Microsoft.Extensions.DependencyInjection;

namespace GitOut.Features.ReleaseNotes
{
    public static class ReleaseNotesServiceCollectionExtensions
    {
        public static void AddReleaseNotesFeature(this IServiceCollection services)
        {
            services.AddTransient<ReleaseNotesPage>();
            services.AddTransient<ReleaseNotesViewModel>();
        }
    }
}
