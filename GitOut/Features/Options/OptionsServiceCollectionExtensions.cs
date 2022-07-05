using GitOut.Features.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace GitOut.Features.Options
{
    public static class OptionsServiceCollectionExtensions
    {
        public static WritableOptionsBuilder<T> AddWritableOptions<T>(this IServiceCollection services) where T : class, new() => new(services);
    }

    public class WritableOptionsBuilder<T> where T : class, new()
    {
        private readonly IServiceCollection services;

        public WritableOptionsBuilder(IServiceCollection services) => this.services = services;

        public void Bind(IConfiguration config, string section)
        {
            services.AddOptions<T>().Bind(config.GetSection(section));
            services.AddScoped<IOptionsWriter<T>>((provider) => new OptionsWriter<T>(
                provider.GetRequiredService<IOptions<T>>(),
                provider.GetRequiredService<IWritableStorage>(),
                section
            ));
        }
    }
}
