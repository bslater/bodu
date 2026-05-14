// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateServiceOptions.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Bundles the optional configuration accepted by <see cref="NotableDateService" />, keeping the constructor surface
/// to three parameters — rule providers, working week, and this options bag.
/// </summary>
/// <remarks>
/// <para>
/// Every property is independently optional. Where a property is <see langword="null" /> the service uses its
/// default behaviour for that concern (for example, <see cref="CollisionResolver" /> defaults to
/// <see cref="DefaultNotableDateCollisionResolver" />). Construct an instance with a C# 9 object initializer and
/// pass it to the relevant <see cref="NotableDateService" /> constructor.
/// </para>
/// <example>
/// <code language="csharp">
/// <![CDATA[
/// var options = new NotableDateServiceOptions
/// {
///     OverrideProviders = new[] { myOverrideProvider },
///     AlgorithmRegistry = new NotableDateAlgorithmRegistry()
///         .Register("easter-sunday", new GregorianEasterSundayNotableDateProvider()),
/// };
///
/// var service = new NotableDateService(ruleProviders, WeekPattern.Weekdays, options);
/// ]]>
/// </code>
/// </example>
/// </remarks>
public sealed class NotableDateServiceOptions
{
    /// <summary>
    /// Gets the optional resource path resolver used to locate embedded XML rule files.
    /// </summary>
    /// <value>An <see cref="IResourcePathResolver" />, or <see langword="null" /> to use the default <see cref="ResourcePathResolver" />.</value>
    /// <returns>The configured resolver, or <see langword="null" /> when defaults apply.</returns>
    public IResourcePathResolver? ResourcePathResolver { get; init; }

    /// <summary>
    /// Gets the optional layered override providers, applied after the base rules in registration order.
    /// </summary>
    /// <value>A sequence of <see cref="INotableDateRuleOverrideProvider" />, or <see langword="null" /> for no overrides.</value>
    /// <returns>The configured override providers, or <see langword="null" /> when no overrides are layered.</returns>
    public IEnumerable<INotableDateRuleOverrideProvider>? OverrideProviders { get; init; }

    /// <summary>
    /// Gets the optional registry used to resolve <see cref="DateResolutionStrategy.Algorithm" /> rules.
    /// </summary>
    /// <value>An <see cref="INotableDateAlgorithmRegistry" />, or <see langword="null" /> for no algorithm dispatch.</value>
    /// <returns>The configured registry, or <see langword="null" /> when no algorithm registrations are supplied.</returns>
    public INotableDateAlgorithmRegistry? AlgorithmRegistry { get; init; }

    /// <summary>
    /// Gets the optional registry of custom <see cref="IAdjustmentHandler" /> instances looked up by key.
    /// </summary>
    /// <value>An <see cref="IAdjustmentHandlerRegistry" />, or <see langword="null" /> for no custom adjustments.</value>
    /// <returns>The configured handler registry, or <see langword="null" /> when no custom handlers are supplied.</returns>
    public IAdjustmentHandlerRegistry? AdjustmentHandlers { get; init; }

    /// <summary>
    /// Gets the optional collision resolver that arbitrates same-day rule collisions.
    /// </summary>
    /// <value>An <see cref="INotableDateCollisionResolver" />, or <see langword="null" /> to use <see cref="DefaultNotableDateCollisionResolver" />.</value>
    /// <returns>The configured collision resolver, or <see langword="null" /> when the default is used.</returns>
    public INotableDateCollisionResolver? CollisionResolver { get; init; }

    /// <summary>
    /// Gets the optional localizer used to translate notable date names into the active culture.
    /// </summary>
    /// <value>An <see cref="INotableDateNameLocalizer" />, or <see langword="null" /> for no localization.</value>
    /// <returns>The configured localizer, or <see langword="null" /> when names are emitted verbatim.</returns>
    public INotableDateNameLocalizer? NameLocalizer { get; init; }

    /// <summary>
    /// Gets the optional external plugins loaded via <see cref="Plugins.ExternalPluginLoader" />.
    /// </summary>
    /// <value>A sequence of <see cref="Plugins.INotableDatePlugin" />, or <see langword="null" /> for no plugins.</value>
    /// <returns>The configured plugins, or <see langword="null" /> when no plugins are loaded.</returns>
    /// <remarks>
    /// <para>
    /// Rule providers exposed by plugins are appended to the constructor's <c>ruleProviders</c> argument and participate
    /// in the normal flatten pipeline. Named algorithms supplied by plugins are registered onto an internal algorithm
    /// registry that falls back to <see cref="AlgorithmRegistry" /> when supplied (the explicit
    /// <see cref="AlgorithmRegistry" /> wins on key collision).
    /// </para>
    /// </remarks>
    public IEnumerable<Plugins.INotableDatePlugin>? Plugins { get; init; }
}
