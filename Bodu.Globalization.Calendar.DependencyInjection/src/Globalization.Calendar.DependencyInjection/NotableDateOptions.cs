// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.DependencyInjection;

/// <summary>
/// Provides the bindable options bag consumed by
/// <see cref="ServiceCollectionExtensions.AddNotableDates(Microsoft.Extensions.DependencyInjection.IServiceCollection, Microsoft.Extensions.Configuration.IConfiguration?, string)" />.
/// </summary>
/// <remarks>
/// <para>
/// This type is intentionally composed of primitives and enums so that the entire surface can be populated from an
/// <c>appsettings.json</c> section via <see cref="Microsoft.Extensions.Configuration.IConfiguration" /> binding. It is
/// distinct from <see cref="NotableDateServiceOptions" />, which carries non-bindable interface-typed dependencies
/// (algorithm registries, collision resolvers, name localisers, plugins). Use <see cref="NotableDateOptions" /> for
/// values shaped by app configuration, and the builder's fluent extension methods for everything else.
/// </para>
/// <para>
/// The default values match the parameterless <see cref="NotableDateService" /> constructor:
/// <see cref="WorkingDaysOfWeek.MondayToFriday" /> working week, no preferred territory, no preferred calendar, no
/// ambient default assignment. Consumers can override any subset via configuration binding, an explicit
/// <c>Configure(o =&gt; ...)</c> callback, or the <c>PostConfigure((sp, o) =&gt; ...)</c> hook for projecting from
/// custom POCOs.
/// </para>
/// </remarks>
/// <example>
/// <para>
/// Binding from <c>appsettings.json</c>:
/// </para>
/// <code language="json">
/// {
///   "NotableDates": {
///     "WorkingDays": "MondayToFriday",
///     "DefaultTerritoryCode": "AU-NSW",
///     "RegisterAsAmbientDefault": true
///   }
/// }
/// </code>
/// </example>
public sealed class NotableDateOptions
{
    /// <summary>
    /// Gets or sets the named working-week pattern applied when classifying dates as working or non-working.
    /// </summary>
    /// <value>
    /// A <see cref="WorkingDaysOfWeek" /> value. Defaults to <see cref="WorkingDaysOfWeek.MondayToFriday" />.
    /// </value>
    /// <returns>The configured working-week preset.</returns>
    /// <remarks>
    /// Translated to a <see cref="WeekPattern" /> by <see cref="WorkingDaysOfWeek" />'s extension methods before being
    /// passed to the <see cref="NotableDateService" /> constructor. The value
    /// <see cref="WorkingDaysOfWeek.Custom" /> has no canonical pattern and will trigger an
    /// <see cref="ArgumentException" /> during registration — use a builder-supplied <see cref="WeekPattern" /> via
    /// <c>UseWorkingWeek</c> instead.
    /// </remarks>
    public WorkingDaysOfWeek WorkingDays { get; set; } = WorkingDaysOfWeek.MondayToFriday;

    /// <summary>
    /// Gets or sets the default territory code applied by extension methods that omit an explicit territory argument.
    /// </summary>
    /// <value>
    /// A territory code (e.g. <c>"AU"</c>, <c>"AU-NSW"</c>, <c>"US"</c>), or <see langword="null" /> for no preferred
    /// territory.
    /// </value>
    /// <returns>The configured default territory, or <see langword="null" /> when none is set.</returns>
    /// <remarks>
    /// <para>
    /// This value is exposed to consumers via <see cref="Microsoft.Extensions.Options.IOptions{TOptions}" />; it is not
    /// pushed into the <see cref="NotableDateService" /> itself because the service supports per-call territory
    /// scoping. Hosts that wish to thread the default territory through every call site should resolve
    /// <c>IOptions&lt;NotableDateOptions&gt;</c> and pass <see cref="DefaultTerritoryCode" /> explicitly.
    /// </para>
    /// </remarks>
    public string? DefaultTerritoryCode { get; set; }

    /// <summary>
    /// Gets or sets the assembly-qualified name of the default calendar type applied by extension methods that omit an
    /// explicit calendar argument.
    /// </summary>
    /// <value>
    /// An assembly-qualified type name resolvable by <see cref="Type.GetType(string)" /> (for example
    /// <c>"System.Globalization.GregorianCalendar"</c>), or <see langword="null" /> for no preferred calendar.
    /// </value>
    /// <returns>The configured default calendar type name, or <see langword="null" /> when none is set.</returns>
    /// <remarks>
    /// Stored as a string so that the value can be bound from configuration. Consumers that need the resolved
    /// <see cref="Type" /> should call <see cref="Type.GetType(string)" /> against this value and fall back to
    /// <see langword="null" /> when the type cannot be resolved at runtime.
    /// </remarks>
    public string? DefaultCalendarTypeName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the resolved <see cref="INotableDateService" /> should also be assigned
    /// to <see cref="NotableDateContext.Default" /> so that extension-method overloads without an explicit service
    /// argument resolve through the DI-registered instance.
    /// </summary>
    /// <value>
    /// <see langword="true" /> to assign the resolved service to the ambient context; otherwise
    /// <see langword="false" />. Defaults to <see langword="false" />.
    /// </value>
    /// <returns>
    /// The configured ambient-default toggle.
    /// </returns>
    /// <remarks>
    /// <para>
    /// Assignment happens exactly once, the first time the <see cref="INotableDateService" /> singleton is resolved
    /// from the container. Assigning <see langword="false" /> after a previous assignment does not unassign — call
    /// <see cref="NotableDateContext.Reset" /> explicitly in tests if you need to restore the ambient default.
    /// </para>
    /// </remarks>
    public bool RegisterAsAmbientDefault { get; set; }
}
