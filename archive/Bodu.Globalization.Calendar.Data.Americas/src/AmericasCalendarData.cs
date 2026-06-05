// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmericasCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides factory entry points for the Americas region calendar data pack — embedded notable-date rules for the
/// United States and Canada — so consumers can compose a <see cref="NotableDateService" /> without knowing the
/// underlying resource layout.
/// </summary>
/// <remarks>
/// <para>
/// The pack ships <c>region-us.xml</c> and <c>region-ca.xml</c> as embedded manifest resources under the historical
/// <c>Bodu.Globalization.Calendar.Resources.*</c> path so that <c>&lt;UseFrom resource="./global-all.xml"&gt;</c>
/// directives inside the region XML resolve against the main library's globals via the provider's assembly chain.
/// </para>
/// <para>
/// Use one of the pack-level entry points (for example <see cref="CreateUnitedStatesProvider" />) when you only need a
/// single country, or <see cref="CreateProviders" /> when you want every country in the pack at once.
/// </para>
/// </remarks>
/// <example>
/// <code>
///<![CDATA[
/// var service = new NotableDateService(
///     ruleProviders: AmericasCalendarData.CreateProviders(),
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);
///]]>
/// </code>
/// </example>
public static class AmericasCalendarData
{
    /// <summary>
    /// The logical resource name for the United States region payload.
    /// </summary>
    public const string UnitedStatesResourceName = "Bodu/Globalization/Calendar/Resources/region-us.xml";

    /// <summary>
    /// The logical resource name for the Canada region payload.
    /// </summary>
    public const string CanadaResourceName = "Bodu/Globalization/Calendar/Resources/region-ca.xml";

    /// <summary>
    /// Gets the assembly that hosts the pack's embedded XML resources. Exposed for advanced scenarios such as building
    /// a custom assembly chain.
    /// </summary>
    /// <returns>
    /// The <see cref="Assembly" /> in which the pack's region payloads are embedded. Never <see langword="null" />.
    /// </returns>
    public static Assembly DataAssembly => typeof(AmericasCalendarData).Assembly;

    /// <summary>
    /// Creates an <see cref="INotableDateRuleProvider" /> for the United States region payload, configured with an
    /// assembly chain that resolves region-local resources from the pack and global cherry-pick targets from the main
    /// library.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateUnitedStatesProvider() =>
        CreateProvider(UnitedStatesResourceName);

    /// <summary>
    /// Creates an <see cref="INotableDateRuleProvider" /> for the Canada region payload, configured with an assembly
    /// chain that resolves region-local resources from the pack and global cherry-pick targets from the main library.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateCanadaProvider() =>
        CreateProvider(CanadaResourceName);

    /// <summary>
    /// Creates one provider per country in the Americas pack so the full set can be passed straight to the
    /// <see cref="NotableDateService" /> constructor's <c>ruleProviders</c> parameter.
    /// </summary>
    /// <returns>An ordered enumeration of providers — one per country in the pack.</returns>
    public static IEnumerable<INotableDateRuleProvider> CreateProviders()
    {
        yield return CreateUnitedStatesProvider();
        yield return CreateCanadaProvider();
    }

    /// <summary>
    /// Creates an <see cref="INotableDateRuleProvider" /> rooted at <paramref name="resourceName" /> using the standard
    /// pack → main-library assembly chain. Exposed to support consumers loading a specific resource by name.
    /// </summary>
    /// <param name="resourceName">
    /// The logical resource name of the root XML payload. Must not be <see langword="null" />.
    /// </param>
    /// <returns>A configured rule provider.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="resourceName" /> is <see langword="null" />.
    /// </exception>
    public static INotableDateRuleProvider CreateProvider(string resourceName) =>
        new XmlResourceNotableDateRuleProvider(
            resourceName,
            new ResourcePathResolver(),
            [DataAssembly, typeof(NotableDateService).Assembly]);
}
