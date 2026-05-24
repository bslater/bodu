// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceBuilderExtensions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar.Plugins;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Provides fluent extension methods that contribute rule providers, override providers, plugins, and supporting
/// services into an <see cref="INotableDateServiceBuilder" />.
/// </summary>
/// <remarks>
/// <para>
/// Each method returns the same <see cref="INotableDateServiceBuilder" /> instance so that contributions can be
/// chained. The extensions are split into three categories:
/// </para>
/// <list type="bullet">
/// <item>
/// <description>
/// <b>Provider registration</b> — <c>AddRuleProvider</c>, <c>AddRuleProviders</c>, <c>AddOverrideProvider</c>,
/// <c>AddPlugin</c> register collaborators that will be resolved via <see cref="IEnumerable{T}" /> injection at
/// service construction time. Multiple calls accumulate.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Single-instance registration</b> — <c>UseAlgorithmRegistry</c>, <c>UseCollisionResolver</c>,
/// <c>UseNameLocalizer</c>, <c>UseAdjustmentHandlers</c>, <c>UseResourcePathResolver</c> register singletons of
/// optional collaborator interfaces. Later calls overwrite earlier ones.
/// </description>
/// </item>
/// <item>
/// <description>
/// <b>Options shaping</b> — <c>Configure</c> and <c>PostConfigure</c> shape the bindable
/// <see cref="NotableDateOptions" /> instance. <c>PostConfigure</c> is the hook for projecting from
/// consumer-defined POCOs registered separately through the standard
/// <see cref="OptionsServiceCollectionExtensions.Configure{TOptions}(IServiceCollection, Action{TOptions})" />
/// surface.
/// </description>
/// </item>
/// </list>
/// </remarks>
public static class NotableDateServiceBuilderExtensions
{
    /// <summary>
    /// Adds an <see cref="INotableDateRuleProvider" /> instance to the builder so that it contributes base rules to
    /// the resolved <see cref="INotableDateService" />.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="provider">The rule provider. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder AddRuleProvider(this INotableDateServiceBuilder builder, INotableDateRuleProvider provider)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(provider);

        builder.Services.AddSingleton(provider);
        return builder;
    }

    /// <summary>
    /// Adds an <see cref="INotableDateRuleProvider" /> type as a singleton so that it is materialised lazily by the
    /// container.
    /// </summary>
    /// <typeparam name="TProvider">The provider type to register.</typeparam>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder AddRuleProvider<TProvider>(this INotableDateServiceBuilder builder)
        where TProvider : class, INotableDateRuleProvider
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.AddSingleton<INotableDateRuleProvider, TProvider>();
        return builder;
    }

    /// <summary>
    /// Adds an <see cref="INotableDateRuleProvider" /> registered via a factory delegate so that it is constructed with
    /// access to the host's <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="factory">
    /// The factory invoked once when the singleton is resolved. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="factory" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder AddRuleProvider(
        this INotableDateServiceBuilder builder,
        Func<IServiceProvider, INotableDateRuleProvider> factory)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(factory);

        builder.Services.AddSingleton(factory);
        return builder;
    }

    /// <summary>
    /// Adds a sequence of <see cref="INotableDateRuleProvider" /> instances — typically the result of a data pack's
    /// <c>CreateProviders()</c> factory — to the builder.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="providers">
    /// The providers to register. Must not be <see langword="null" />; individual elements must not be
    /// <see langword="null" />.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="providers" /> is <see langword="null" />, or when
    /// any element of <paramref name="providers" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder AddRuleProviders(
        this INotableDateServiceBuilder builder,
        IEnumerable<INotableDateRuleProvider> providers)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(providers);

        foreach (INotableDateRuleProvider provider in providers)
        {
            if (provider is null) throw new ArgumentException("Provider sequence must not contain null elements.", nameof(providers));
            builder.Services.AddSingleton(provider);
        }

        return builder;
    }

    /// <summary>
    /// Adds an <see cref="INotableDateRuleOverrideProvider" /> instance to the builder so that its additions and
    /// removals layer on top of the base rule set.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="provider">The override provider. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="provider" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// When <paramref name="provider" /> is a <see cref="MutableNotableDateRuleOverrideProvider" />, the resolved
    /// <see cref="INotableDateService" /> factory subscribes <see cref="INotableDateService.Reload" /> to the
    /// provider's <see cref="MutableNotableDateRuleOverrideProvider.Changed" /> event so that runtime mutations take
    /// effect without further intervention.
    /// </para>
    /// </remarks>
    public static INotableDateServiceBuilder AddOverrideProvider(this INotableDateServiceBuilder builder, INotableDateRuleOverrideProvider provider)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(provider);

        builder.Services.AddSingleton(provider);
        return builder;
    }

    /// <summary>
    /// Adds an <see cref="INotableDateRuleOverrideProvider" /> type as a singleton so that it is materialised lazily
    /// by the container.
    /// </summary>
    /// <typeparam name="TOverride">The override provider type to register.</typeparam>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder AddOverrideProvider<TOverride>(this INotableDateServiceBuilder builder)
        where TOverride : class, INotableDateRuleOverrideProvider
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.AddSingleton<INotableDateRuleOverrideProvider, TOverride>();
        return builder;
    }

    /// <summary>
    /// Adds an <see cref="INotableDatePlugin" /> instance to the builder so that its rule providers and algorithm
    /// contributions participate in the resolved service.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="plugin">The plugin. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="plugin" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder AddPlugin(this INotableDateServiceBuilder builder, INotableDatePlugin plugin)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(plugin);

        builder.Services.AddSingleton(plugin);
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="INotableDateAlgorithmRegistry" /> singleton consulted by the resolved service when
    /// dispatching <see cref="DateResolutionStrategy.Algorithm" /> rules.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="registry">The algorithm registry. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="registry" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// A subsequent call replaces a previously registered registry. Pair with <see cref="AddPlugin" /> to compose
    /// algorithm contributions from multiple sources.
    /// </remarks>
    public static INotableDateServiceBuilder UseAlgorithmRegistry(this INotableDateServiceBuilder builder, INotableDateAlgorithmRegistry registry)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(registry);

        builder.Services.Replace(ServiceDescriptor.Singleton(registry));
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="INotableDateCollisionResolver" /> singleton that arbitrates same-day rule collisions.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="resolver">The collision resolver. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="resolver" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder UseCollisionResolver(this INotableDateServiceBuilder builder, INotableDateCollisionResolver resolver)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(resolver);

        builder.Services.Replace(ServiceDescriptor.Singleton(resolver));
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="INotableDateNameLocalizer" /> singleton that translates notable-date names into the
    /// active culture.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="localizer">The name localiser. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="localizer" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder UseNameLocalizer(this INotableDateServiceBuilder builder, INotableDateNameLocalizer localizer)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(localizer);

        builder.Services.Replace(ServiceDescriptor.Singleton(localizer));
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IAdjustmentHandlerRegistry" /> singleton that supplies custom observance-adjustment
    /// handlers.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="registry">The adjustment-handler registry. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="registry" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder UseAdjustmentHandlers(this INotableDateServiceBuilder builder, IAdjustmentHandlerRegistry registry)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(registry);

        builder.Services.Replace(ServiceDescriptor.Singleton(registry));
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IResourcePathResolver" /> singleton that locates embedded XML rule files for resource
    /// providers.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="resolver">The resource-path resolver. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="resolver" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder UseResourcePathResolver(this INotableDateServiceBuilder builder, IResourcePathResolver resolver)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(resolver);

        builder.Services.Replace(ServiceDescriptor.Singleton(resolver));
        return builder;
    }

    /// <summary>
    /// Layers an additional configuration callback onto <see cref="NotableDateOptions" /> so that programmatic
    /// settings overlay any value bound from <see cref="Microsoft.Extensions.Configuration.IConfiguration" />.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="configure">
    /// The configuration callback. Invoked with the <see cref="NotableDateOptions" /> instance about to be resolved.
    /// Must not be <see langword="null" />.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder Configure(this INotableDateServiceBuilder builder, Action<NotableDateOptions> configure)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configure);

        builder.Services.Configure(configure);
        return builder;
    }

    /// <summary>
    /// Registers an <see cref="IPostConfigureOptions{TOptions}" /> callback that runs after every other configuration
    /// source has populated <see cref="NotableDateOptions" /> and has access to the host's
    /// <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="configure">
    /// The post-configure callback. Receives the resolved <see cref="IServiceProvider" /> and the populated
    /// <see cref="NotableDateOptions" /> instance. Must not be <see langword="null" />.
    /// </param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> or <paramref name="configure" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// <para>
    /// This is the consumer-defined-options hook. A consumer who has their own bindable POCO can register it normally
    /// via <see cref="OptionsServiceCollectionExtensions.Configure{TOptions}(IServiceCollection, Action{TOptions})" />
    /// and then project values into <see cref="NotableDateOptions" /> from inside this callback. The same pattern is
    /// used by EF Core (<c>AddDbContext((sp, builder) =&gt; ...)</c>), ASP.NET Core authentication, and
    /// <c>AddHttpClient</c>.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    ///<![CDATA[
    /// services.Configure<MyAppCalendarSettings>(configuration.GetSection("MyApp:Calendar"));
    /// services.AddNotableDates()
    ///     .PostConfigure((sp, opts) =>
    ///     {
    ///         MyAppCalendarSettings custom = sp.GetRequiredService<IOptions<MyAppCalendarSettings>>().Value;
    ///         opts.DefaultTerritoryCode = custom.RegionCode;
    ///         opts.WorkingDays = custom.WorkWeek;
    ///     });
    ///]]>
    /// </code>
    /// </example>
    public static INotableDateServiceBuilder PostConfigure(
        this INotableDateServiceBuilder builder,
        Action<IServiceProvider, NotableDateOptions> configure)
    {
        ThrowHelper.ThrowIfNull(builder);
        ThrowHelper.ThrowIfNull(configure);

        builder.Services.AddSingleton<IPostConfigureOptions<NotableDateOptions>>(sp =>
            new PostConfigureOptionsWithProvider(sp, configure));
        return builder;
    }

    /// <summary>
    /// Overrides the working-week preset that the resolved <see cref="INotableDateService" /> classifies dates
    /// against.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <param name="workingDays">The working-week preset.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateServiceBuilder UseWorkingDays(this INotableDateServiceBuilder builder, WorkingDaysOfWeek workingDays)
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.Configure<NotableDateOptions>(opts => opts.WorkingDays = workingDays);
        return builder;
    }

    /// <summary>
    /// Toggles whether the resolved <see cref="INotableDateService" /> is also assigned to
    /// <see cref="NotableDateContext.Default" />.
    /// </summary>
    /// <param name="builder">The builder. Must not be <see langword="null" />.</param>
    /// <returns>The same builder, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="builder" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// Equivalent to <c>builder.Configure(opts =&gt; opts.RegisterAsAmbientDefault = true)</c>. The ambient assignment
    /// is performed lazily the first time the singleton is resolved.
    /// </remarks>
    public static INotableDateServiceBuilder RegisterAsAmbientDefault(this INotableDateServiceBuilder builder)
    {
        ThrowHelper.ThrowIfNull(builder);

        builder.Services.Configure<NotableDateOptions>(opts => opts.RegisterAsAmbientDefault = true);
        return builder;
    }

    /// <summary>
    /// Closure-bearing adapter that lets <see cref="PostConfigure" /> register an
    /// <see cref="IPostConfigureOptions{TOptions}" /> backed by a delegate that needs both an
    /// <see cref="IServiceProvider" /> and the <see cref="NotableDateOptions" /> instance.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="IPostConfigureOptions{TOptions}" /> has no built-in IServiceProvider-aware overload, so we capture
    /// the provider at registration time (via the factory invoked when the adapter singleton is materialised) and
    /// hand it to the consumer callback on each post-configure invocation.
    /// </para>
    /// </remarks>
    private sealed class PostConfigureOptionsWithProvider : IPostConfigureOptions<NotableDateOptions>
    {
        /// <summary>
        /// The service provider captured at registration time.
        /// </summary>
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// The consumer-supplied callback invoked on every post-configure pass.
        /// </summary>
        private readonly Action<IServiceProvider, NotableDateOptions> _configure;

        /// <summary>
        /// Initializes a new instance of the <see cref="PostConfigureOptionsWithProvider" /> class.
        /// </summary>
        /// <param name="serviceProvider">The captured service provider.</param>
        /// <param name="configure">The consumer-supplied post-configure callback.</param>
        public PostConfigureOptionsWithProvider(IServiceProvider serviceProvider, Action<IServiceProvider, NotableDateOptions> configure)
        {
            _serviceProvider = serviceProvider;
            _configure = configure;
        }

        /// <inheritdoc />
        public void PostConfigure(string? name, NotableDateOptions options)
        {
            _configure(_serviceProvider, options);
        }
    }
}
