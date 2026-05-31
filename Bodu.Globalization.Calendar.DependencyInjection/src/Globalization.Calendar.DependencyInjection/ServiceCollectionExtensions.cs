// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ServiceCollectionExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;
using Bodu.Globalization.Calendar.Plugins;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Provides the <c>AddNotableDates</c> entry points that register an <see cref="INotableDateService" /> singleton and
/// its supporting infrastructure into an <see cref="IServiceCollection" />.
/// </summary>
/// <remarks>
/// <para>
/// The extensions follow the conventions established by <c>Microsoft.Extensions.*</c> packages: registration returns a
/// builder (<see cref="INotableDateServiceBuilder" />) that exposes a fluent surface for layering in rule providers,
/// override providers, plugins, algorithm registries, collision resolvers, name localisers, and options projection. All
/// collaborator interfaces are registered via <c>IEnumerable&lt;T&gt;</c> injection so that consumers can compose
/// contributions from multiple data packs without overwriting earlier registrations.
/// </para>
/// <para>
/// Two registration shapes are supported: configuration-driven (binds an <see cref="IConfiguration" /> section into
/// <see cref="NotableDateOptions" />) and builder-callback driven (programmatic composition). Both return the same
/// <see cref="INotableDateServiceBuilder" /> so registrations remain chainable regardless of entry point.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Idiomatic ASP.NET Core registration with an Asia-Pacific data pack and runtime overrides:
/// </para>
/// <code>
///<![CDATA[
/// builder.Services
///     .AddNotableDates(builder.Configuration)
///     .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
///     .AddOverrideProvider(new MutableNotableDateRuleOverrideProvider())
///     .RegisterAsAmbientDefault();
///]]>
/// </code>
/// </example>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// The default <see cref="IConfiguration" /> section name read by
    /// <see cref="AddNotableDates(IServiceCollection, IConfiguration?, string)" /> when no override is supplied.
    /// </summary>
    public const string DefaultConfigurationSection = "NotableDates";

    /// <summary>
    /// Registers an <see cref="INotableDateService" /> singleton and its supporting infrastructure into
    /// <paramref name="services" />, optionally binding <see cref="NotableDateOptions" /> from
    /// <paramref name="configuration" />.
    /// </summary>
    /// <param name="services">The service collection to register into. Must not be <see langword="null" />.</param>
    /// <param name="configuration">
    /// Optional <see cref="IConfiguration" /> root or section. When supplied, the section identified by
    /// <paramref name="sectionName" /> is bound into <see cref="NotableDateOptions" />. When <see langword="null" />,
    /// only programmatic configuration (via the returned builder) populates the options.
    /// </param>
    /// <param name="sectionName">
    /// The configuration section name to bind from <paramref name="configuration" />. Ignored when
    /// <paramref name="configuration" /> is <see langword="null" />. Defaults to
    /// <see cref="DefaultConfigurationSection" />.
    /// </param>
    /// <returns>
    /// An <see cref="INotableDateServiceBuilder" /> for further registration via the fluent extension methods in
    /// <see cref="NotableDateServiceBuilderExtensions" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="sectionName" /> is empty or whitespace.
    /// </exception>
    /// <remarks>
    /// <para>
    /// Repeated calls to <see cref="AddNotableDates(IServiceCollection, IConfiguration?, string)" /> are safe: the
    /// underlying <see cref="INotableDateService" /> factory is registered idempotently via
    /// <see cref="ServiceCollectionDescriptorExtensions.TryAddSingleton{TService}(IServiceCollection, Func{IServiceProvider, TService})" />
    /// , so the singleton resolves to the same instance regardless of how many times the method is called. Repeated
    /// calls layer additional configuration binding sources onto <see cref="NotableDateOptions" />.
    /// </para>
    /// </remarks>
    public static INotableDateServiceBuilder AddNotableDates(
        this IServiceCollection services,
        IConfiguration? configuration = null,
        string sectionName = DefaultConfigurationSection)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        services.AddOptions();

        if (configuration is not null)
        {
            services
                .AddOptions<NotableDateOptions>()
                .Bind(configuration.GetSection(sectionName));
        }

        RegisterServiceFactory(services);

        return new NotableDateServiceBuilder(services);
    }

    /// <summary>
    /// Registers an <see cref="INotableDateService" /> singleton into <paramref name="services" /> and invokes the
    /// supplied builder callback for programmatic composition.
    /// </summary>
    /// <param name="services">The service collection to register into. Must not be <see langword="null" />.</param>
    /// <param name="configure">
    /// A callback invoked with the <see cref="INotableDateServiceBuilder" /> for fluent registration of rule providers,
    /// override providers, and supporting services. Must not be <see langword="null" />.
    /// </param>
    /// <returns>
    /// The same <see cref="INotableDateServiceBuilder" /> the callback received, returned for callers that wish to
    /// continue chaining outside the callback.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder AddNotableDates(
        this IServiceCollection services,
        Action<INotableDateServiceBuilder> configure)
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(configure);

        INotableDateServiceBuilder builder = services.AddNotableDates();
        configure(builder);
        return builder;
    }

    /// <summary>
    /// Registers the <see cref="INotableDateService" /> singleton factory exactly once, regardless of how many times
    /// <see cref="AddNotableDates(IServiceCollection, IConfiguration?, string)" /> is called.
    /// </summary>
    /// <param name="services">The service collection to register into.</param>
    /// <remarks>
    /// <para>
    /// The factory composes a <see cref="NotableDateService" /> from the registered <see cref="IEnumerable{T}" /> of
    /// rule providers, override providers, and plugins, augmented with optional single-instance registrations of
    /// <see cref="INotableDateAlgorithmRegistry" />, <see cref="INotableDateCollisionResolver" />,
    /// <see cref="INotableDateNameLocalizer" />, <see cref="IAdjustmentHandlerRegistry" />, and
    /// <see cref="IResourcePathResolver" />.
    /// </para>
    /// <para>
    /// When <see cref="NotableDateOptions.RegisterAsAmbientDefault" /> is <see langword="true" />, the resolved service
    /// is also assigned to <see cref="NotableDateContext.Default" /> the first time the singleton is resolved. The
    /// assignment is performed under the singleton factory's intrinsic single-execution semantics and so happens at
    /// most once.
    /// </para>
    /// </remarks>
    private static void RegisterServiceFactory(IServiceCollection services)
    {
        services.TryAddSingleton<INotableDateService>(provider =>
        {
            NotableDateOptions opts = provider.GetRequiredService<IOptions<NotableDateOptions>>().Value;

            IEnumerable<INotableDateRuleProvider> ruleProviders =
                provider.GetServices<INotableDateRuleProvider>();
            IEnumerable<INotableDateRuleOverrideProvider> overrideProviders =
                provider.GetServices<INotableDateRuleOverrideProvider>();
            IEnumerable<INotableDatePlugin> plugins =
                provider.GetServices<INotableDatePlugin>();

            NotableDateServiceOptions serviceOptions = new()
            {
                OverrideProviders = overrideProviders.ToArray(),
                AlgorithmRegistry = provider.GetService<INotableDateAlgorithmRegistry>(),
                AdjustmentHandlers = provider.GetService<IAdjustmentHandlerRegistry>(),
                CollisionResolver = provider.GetService<INotableDateCollisionResolver>(),
                NameLocalizer = provider.GetService<INotableDateNameLocalizer>(),
                ResourcePathResolver = provider.GetService<IResourcePathResolver>(),
                Plugins = plugins.ToArray(),
            };

            var workingWeek = opts.WorkingDays.ToWeekPattern();

            NotableDateService service = new(ruleProviders.ToArray(), workingWeek, serviceOptions);

            foreach (INotableDateRuleOverrideProvider op in overrideProviders)
            {
                if (op is MutableNotableDateRuleOverrideProvider mutable)
                {
                    mutable.Changed += (_, _) => service.Reload();
                }
            }

            if (opts.RegisterAsAmbientDefault)
            {
                NotableDateContext.Default = service;
            }

            return service;
        });
    }
}
