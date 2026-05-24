// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateServiceBuilder.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Defines the fluent registration surface returned by
/// <see cref="ServiceCollectionExtensions.AddNotableDates(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)" />
/// so that consumers can register rule providers, override providers, and supporting services on the same
/// <see cref="IServiceCollection" /> in a discoverable manner.
/// </summary>
/// <remarks>
/// <para>
/// The interface exposes a single <see cref="Services" /> property that returns the underlying
/// <see cref="IServiceCollection" />. All fluent functionality is provided through extension methods defined in
/// <see cref="NotableDateServiceBuilderExtensions" />, mirroring the pattern used by <c>IHttpClientBuilder</c>,
/// <c>IMvcBuilder</c>, and <c>IAuthenticationBuilder</c> in <c>Microsoft.Extensions.*</c>. This keeps the abstraction
/// minimal while remaining trivially extensible by third-party packages.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// A typical end-to-end registration with a data pack, runtime overrides, and ambient-default assignment:
/// </para>
/// <code>
///<![CDATA[
/// services
///     .AddNotableDates(configuration)
///     .AddRuleProviders(AsiaPacificCalendarData.CreateProviders())
///     .AddOverrideProvider(new MutableNotableDateRuleOverrideProvider())
///     .UseWorkingDays(WorkingDaysOfWeek.MondayToFriday)
///     .RegisterAsAmbientDefault();
///]]>
/// </code>
/// </example>
public interface INotableDateServiceBuilder
{
    /// <summary>
    /// Gets the <see cref="IServiceCollection" /> that the builder is composing into.
    /// </summary>
    /// <returns>
    /// The service collection passed to
    /// <see cref="ServiceCollectionExtensions.AddNotableDates(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)" />.
    /// </returns>
    IServiceCollection Services { get; }
}
