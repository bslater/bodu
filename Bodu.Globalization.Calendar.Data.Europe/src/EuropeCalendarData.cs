// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarData.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Data.Europe;

/// <summary>
/// Provides factory entry points for the Europe region calendar data pack — embedded notable-date rules for Germany,
/// Spain, France, the United Kingdom, Ireland, Italy, the Netherlands, and Sweden — so consumers can compose a
/// <see cref="NotableDateService" /> without knowing the underlying resource layout.
/// </summary>
/// <remarks>
/// <para>
/// Each region XML is embedded under the historical <c>Bodu.Globalization.Calendar.Resources.*</c> path so that
/// <c>&lt;UseFrom resource="./global-all.xml"&gt;</c> directives resolve against the main library's globals via the
/// provider's assembly chain.
/// </para>
/// <para>
/// Use a country-specific factory (for example <see cref="CreateUnitedKingdomProvider" />) when you only need a single
/// country, or <see cref="CreateProviders" /> when you want every country in the pack at once.
/// </para>
/// </remarks>
/// <example>
/// <code>
///<![CDATA[
/// var service = new NotableDateService( ruleProviders: EuropeCalendarData.CreateProviders(), workingDaysOfWeek:
/// WorkingDaysOfWeek.MondayToFriday);
///]]>
/// </code>
/// </example>
public static class EuropeCalendarData
{
    /// <summary>
    /// The logical resource name for the Germany region payload.
    /// </summary>
    public const string GermanyResourceName = "Bodu/Globalization/Calendar/Resources/region-de.xml";

    /// <summary>
    /// The logical resource name for the Spain region payload.
    /// </summary>
    public const string SpainResourceName = "Bodu/Globalization/Calendar/Resources/region-es.xml";

    /// <summary>
    /// The logical resource name for the France region payload.
    /// </summary>
    public const string FranceResourceName = "Bodu/Globalization/Calendar/Resources/region-fr.xml";

    /// <summary>
    /// The logical resource name for the United Kingdom region payload.
    /// </summary>
    public const string UnitedKingdomResourceName = "Bodu/Globalization/Calendar/Resources/region-gb.xml";

    /// <summary>
    /// The logical resource name for the Ireland region payload.
    /// </summary>
    public const string IrelandResourceName = "Bodu/Globalization/Calendar/Resources/region-ie.xml";

    /// <summary>
    /// The logical resource name for the Italy region payload.
    /// </summary>
    public const string ItalyResourceName = "Bodu/Globalization/Calendar/Resources/region-it.xml";

    /// <summary>
    /// The logical resource name for the Netherlands region payload.
    /// </summary>
    public const string NetherlandsResourceName = "Bodu/Globalization/Calendar/Resources/region-nl.xml";

    /// <summary>
    /// The logical resource name for the Sweden region payload.
    /// </summary>
    public const string SwedenResourceName = "Bodu/Globalization/Calendar/Resources/region-se.xml";

    /// <summary>
    /// Gets the assembly that hosts the pack's embedded XML resources. Exposed for advanced scenarios such as building
    /// a custom assembly chain.
    /// </summary>
    /// <returns>
    /// The <see cref="Assembly" /> in which the pack's region payloads are embedded. Never <see langword="null" />.
    /// </returns>
    public static Assembly DataAssembly => typeof(EuropeCalendarData).Assembly;

    /// <summary>
    /// Creates a Germany rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateGermanyProvider() => CreateProvider(GermanyResourceName);

    /// <summary>
    /// Creates a Spain rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSpainProvider() => CreateProvider(SpainResourceName);

    /// <summary>
    /// Creates a France rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateFranceProvider() => CreateProvider(FranceResourceName);

    /// <summary>
    /// Creates a United Kingdom rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateUnitedKingdomProvider() => CreateProvider(UnitedKingdomResourceName);

    /// <summary>
    /// Creates an Ireland rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateIrelandProvider() => CreateProvider(IrelandResourceName);

    /// <summary>
    /// Creates an Italy rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateItalyProvider() => CreateProvider(ItalyResourceName);

    /// <summary>
    /// Creates a Netherlands rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateNetherlandsProvider() => CreateProvider(NetherlandsResourceName);

    /// <summary>
    /// Creates a Sweden rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSwedenProvider() => CreateProvider(SwedenResourceName);

    /// <summary>
    /// Creates one provider per country in the Europe pack so the full set can be passed straight to the
    /// <see cref="NotableDateService" /> constructor's <c>ruleProviders</c> parameter.
    /// </summary>
    /// <returns>An ordered enumeration of providers — one per country in the pack.</returns>
    public static IEnumerable<INotableDateRuleProvider> CreateProviders()
    {
        yield return CreateGermanyProvider();
        yield return CreateSpainProvider();
        yield return CreateFranceProvider();
        yield return CreateUnitedKingdomProvider();
        yield return CreateIrelandProvider();
        yield return CreateItalyProvider();
        yield return CreateNetherlandsProvider();
        yield return CreateSwedenProvider();
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
            new[] { DataAssembly, typeof(NotableDateService).Assembly });
}
