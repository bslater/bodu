// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationOptionsExtensions.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Extensions.Configuration.Text;

/// <summary>
/// Provides convenience extension methods for binding a Bodu Text Configuration section to an
/// <see cref="Microsoft.Extensions.Options.IOptions{TOptions}" /> instance through an <see cref="IServiceCollection" />
/// .
/// </summary>
/// <remarks>
/// <para>
/// These helpers are thin shims over
/// <c>Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure</c>; they
/// exist purely to keep the call-site short and discoverable for consumers who reach for an <c>AddConfiguration*</c>
/// API by name. Callers comfortable with <see cref="Microsoft.Extensions.Options.IOptions{TOptions}" /> binding may
/// continue to call
/// <see cref="Microsoft.Extensions.DependencyInjection.OptionsConfigurationServiceCollectionExtensions.Configure{TOptions}(IServiceCollection, IConfiguration)" />
/// directly.
/// </para>
/// <para>
/// Two overloads are provided: one that takes the section name and the root
/// <see cref="Microsoft.Extensions.Configuration.IConfiguration" />, and one that takes a pre-resolved
/// <see cref="Microsoft.Extensions.Configuration.IConfigurationSection" />. Both produce the same registration; pick
/// whichever shape is more natural at the call site.
/// </para>
/// </remarks>
/// <example>
///<![CDATA[
/// Strongly-typed options bound to the "Logging" section produced by AddConfiguration.
/// public sealed class LoggingOptions
/// {
///     public string Level { get; init; } = "Information";
///     public bool   IncludeScopes { get; init; }
/// }
///
/// var builder = WebApplication.CreateBuilder(args);
/// builder.Configuration.AddConfiguration("appsettings.boduconfig");
///
/// Bind by section name against the configuration root.
/// builder.Services.AddConfigurationOptions<LoggingOptions>(
///     builder.Configuration,
///     sectionName: "Logging");
///
/// Or bind a pre-resolved section directly.
/// IConfigurationSection logging = builder.Configuration.GetSection("Logging");
/// builder.Services.AddConfigurationOptions<LoggingOptions>(logging);
///
/// Consumed via constructor injection.
/// public sealed class HomeController(IOptions<LoggingOptions> options) { /* options.Value.Level */ }
///]]>
/// </example>
public static class ConfigurationOptionsExtensions
{
    /// <summary>
    /// Registers an <typeparamref name="TOptions" /> binding against the section named <paramref name="sectionName" />
    /// in <paramref name="configuration" />.
    /// </summary>
    /// <typeparam name="TOptions">The options type to bind.</typeparam>
    /// <param name="services">The service collection to register the binding with.</param>
    /// <param name="configuration">The configuration root that contains the named section.</param>
    /// <param name="sectionName">The colon-delimited section name to bind from.</param>
    /// <returns>The supplied <paramref name="services" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="configuration" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// <paramref name="sectionName" /> is <see langword="null" />, empty, or whitespace.
    /// </exception>
    public static IServiceCollection AddConfigurationOptions<TOptions>(
        this IServiceCollection services,
        IConfiguration configuration,
        string sectionName)
        where TOptions : class
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(configuration);
        ThrowHelper.ThrowIfNullOrWhiteSpace(sectionName);

        return services.Configure<TOptions>(configuration.GetSection(sectionName));
    }

    /// <summary>
    /// Registers an <typeparamref name="TOptions" /> binding against the supplied configuration
    /// <paramref name="section" />.
    /// </summary>
    /// <typeparam name="TOptions">The options type to bind.</typeparam>
    /// <param name="services">The service collection to register the binding with.</param>
    /// <param name="section">The configuration section to bind from.</param>
    /// <returns>The supplied <paramref name="services" />, for chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="services" /> or <paramref name="section" /> is <see langword="null" />.
    /// </exception>
    public static IServiceCollection AddConfigurationOptions<TOptions>(
        this IServiceCollection services,
        IConfigurationSection section)
        where TOptions : class
    {
        ThrowHelper.ThrowIfNull(services);
        ThrowHelper.ThrowIfNull(section);

        return services.Configure<TOptions>(section);
    }
}
