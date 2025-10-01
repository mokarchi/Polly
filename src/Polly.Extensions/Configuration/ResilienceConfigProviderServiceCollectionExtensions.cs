using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Polly.Configuration;
using Polly.Utils;

namespace Polly;

/// <summary>
/// Extension methods for registering resilience configuration providers with <see cref="IServiceCollection"/>.
/// </summary>
public static class ResilienceConfigProviderServiceCollectionExtensions
{
    /// <summary>
    /// Adds the default resilience configuration provider that reads from <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResilienceConfigProvider(this IServiceCollection services)
    {
        return AddResilienceConfigProvider(services, sectionName: null);
    }

    /// <summary>
    /// Adds the default resilience configuration provider that reads from <see cref="IConfiguration"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="sectionName">The configuration section name to read from. If null, defaults to "ResiliencePipelines".</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResilienceConfigProvider(this IServiceCollection services, string? sectionName)
    {
        Guard.NotNull(services);

        services.TryAddSingleton<IResilienceConfigProvider>(serviceProvider =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            return new DefaultResilienceConfigProvider(configuration, sectionName);
        });

        return services;
    }

    /// <summary>
    /// Adds a custom resilience configuration provider.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="implementationFactory">A factory that creates the configuration provider.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> or <paramref name="implementationFactory"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResilienceConfigProvider(
        this IServiceCollection services,
        Func<IServiceProvider, IResilienceConfigProvider> implementationFactory)
    {
        Guard.NotNull(services);
        Guard.NotNull(implementationFactory);

        services.TryAddSingleton(implementationFactory);

        return services;
    }

    /// <summary>
    /// Adds a custom resilience configuration provider.
    /// </summary>
    /// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResilienceConfigProvider<TImplementation>(this IServiceCollection services)
        where TImplementation : class, IResilienceConfigProvider
    {
        Guard.NotNull(services);

        services.TryAddSingleton<IResilienceConfigProvider, TImplementation>();

        return services;
    }
}