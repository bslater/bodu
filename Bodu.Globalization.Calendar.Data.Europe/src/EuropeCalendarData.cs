// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides factory entry points for the Europe region calendar data pack — embedded notable-date rules for the United
/// Kingdom and all twenty-seven European Union member states — so consumers can compose a
/// <see cref="NotableDateService" /> without knowing the underlying resource layout.
/// </summary>
/// <remarks>
/// <para>
/// Each country XML is embedded under the historical <c>Bodu.Globalization.Calendar.Resources.*</c> path. The dates
/// shared across Europe are defined once in <c>europe-common.xml</c> (itself a curated re-export of the main library's
/// global resources) and inherited by each country through <c>&lt;UseFrom resource="./europe-common.xml"&gt;</c>
/// directives, which resolve against the pack and the main library via the provider's assembly chain.
/// </para>
/// <para>
/// Use a country-specific factory (for example <see cref="CreateUnitedKingdomProvider" />) when you only need a single
/// country, or <see cref="CreateProviders" /> when you want every country in the pack at once.
/// </para>
/// </remarks>
/// <example>
/// <code>
///<![CDATA[
/// var service = new NotableDateService(
///     ruleProviders: EuropeCalendarData.CreateProviders(),
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);
///]]>
/// </code>
/// </example>
public static class EuropeCalendarData
{
    /// <summary>
    /// The logical resource name for the Austria region payload.
    /// </summary>
    public const string AustriaResourceName = "Bodu/Globalization/Calendar/Resources/region-at.xml";

    /// <summary>
    /// The logical resource name for the Belgium region payload.
    /// </summary>
    public const string BelgiumResourceName = "Bodu/Globalization/Calendar/Resources/region-be.xml";

    /// <summary>
    /// The logical resource name for the Bulgaria region payload.
    /// </summary>
    public const string BulgariaResourceName = "Bodu/Globalization/Calendar/Resources/region-bg.xml";

    /// <summary>
    /// The logical resource name for the Croatia region payload.
    /// </summary>
    public const string CroatiaResourceName = "Bodu/Globalization/Calendar/Resources/region-hr.xml";

    /// <summary>
    /// The logical resource name for the Cyprus region payload.
    /// </summary>
    public const string CyprusResourceName = "Bodu/Globalization/Calendar/Resources/region-cy.xml";

    /// <summary>
    /// The logical resource name for the Czechia region payload.
    /// </summary>
    public const string CzechiaResourceName = "Bodu/Globalization/Calendar/Resources/region-cz.xml";

    /// <summary>
    /// The logical resource name for the Denmark region payload.
    /// </summary>
    public const string DenmarkResourceName = "Bodu/Globalization/Calendar/Resources/region-dk.xml";

    /// <summary>
    /// The logical resource name for the Estonia region payload.
    /// </summary>
    public const string EstoniaResourceName = "Bodu/Globalization/Calendar/Resources/region-ee.xml";

    /// <summary>
    /// The logical resource name for the Finland region payload.
    /// </summary>
    public const string FinlandResourceName = "Bodu/Globalization/Calendar/Resources/region-fi.xml";

    /// <summary>
    /// The logical resource name for the France region payload.
    /// </summary>
    public const string FranceResourceName = "Bodu/Globalization/Calendar/Resources/region-fr.xml";

    /// <summary>
    /// The logical resource name for the Germany region payload.
    /// </summary>
    public const string GermanyResourceName = "Bodu/Globalization/Calendar/Resources/region-de.xml";

    /// <summary>
    /// The logical resource name for the Greece region payload.
    /// </summary>
    public const string GreeceResourceName = "Bodu/Globalization/Calendar/Resources/region-gr.xml";

    /// <summary>
    /// The logical resource name for the Hungary region payload.
    /// </summary>
    public const string HungaryResourceName = "Bodu/Globalization/Calendar/Resources/region-hu.xml";

    /// <summary>
    /// The logical resource name for the Ireland region payload.
    /// </summary>
    public const string IrelandResourceName = "Bodu/Globalization/Calendar/Resources/region-ie.xml";

    /// <summary>
    /// The logical resource name for the Italy region payload.
    /// </summary>
    public const string ItalyResourceName = "Bodu/Globalization/Calendar/Resources/region-it.xml";

    /// <summary>
    /// The logical resource name for the Latvia region payload.
    /// </summary>
    public const string LatviaResourceName = "Bodu/Globalization/Calendar/Resources/region-lv.xml";

    /// <summary>
    /// The logical resource name for the Lithuania region payload.
    /// </summary>
    public const string LithuaniaResourceName = "Bodu/Globalization/Calendar/Resources/region-lt.xml";

    /// <summary>
    /// The logical resource name for the Luxembourg region payload.
    /// </summary>
    public const string LuxembourgResourceName = "Bodu/Globalization/Calendar/Resources/region-lu.xml";

    /// <summary>
    /// The logical resource name for the Malta region payload.
    /// </summary>
    public const string MaltaResourceName = "Bodu/Globalization/Calendar/Resources/region-mt.xml";

    /// <summary>
    /// The logical resource name for the Netherlands region payload.
    /// </summary>
    public const string NetherlandsResourceName = "Bodu/Globalization/Calendar/Resources/region-nl.xml";

    /// <summary>
    /// The logical resource name for the Poland region payload.
    /// </summary>
    public const string PolandResourceName = "Bodu/Globalization/Calendar/Resources/region-pl.xml";

    /// <summary>
    /// The logical resource name for the Portugal region payload.
    /// </summary>
    public const string PortugalResourceName = "Bodu/Globalization/Calendar/Resources/region-pt.xml";

    /// <summary>
    /// The logical resource name for the Romania region payload.
    /// </summary>
    public const string RomaniaResourceName = "Bodu/Globalization/Calendar/Resources/region-ro.xml";

    /// <summary>
    /// The logical resource name for the Slovakia region payload.
    /// </summary>
    public const string SlovakiaResourceName = "Bodu/Globalization/Calendar/Resources/region-sk.xml";

    /// <summary>
    /// The logical resource name for the Slovenia region payload.
    /// </summary>
    public const string SloveniaResourceName = "Bodu/Globalization/Calendar/Resources/region-si.xml";

    /// <summary>
    /// The logical resource name for the Spain region payload.
    /// </summary>
    public const string SpainResourceName = "Bodu/Globalization/Calendar/Resources/region-es.xml";

    /// <summary>
    /// The logical resource name for the Sweden region payload.
    /// </summary>
    public const string SwedenResourceName = "Bodu/Globalization/Calendar/Resources/region-se.xml";

    /// <summary>
    /// The logical resource name for the United Kingdom region payload.
    /// </summary>
    public const string UnitedKingdomResourceName = "Bodu/Globalization/Calendar/Resources/region-gb.xml";

    /// <summary>
    /// Gets the assembly that hosts the pack's embedded XML resources. Exposed for advanced scenarios such as building
    /// a custom assembly chain.
    /// </summary>
    /// <returns>
    /// The <see cref="Assembly" /> in which the pack's region payloads are embedded. Never <see langword="null" />.
    /// </returns>
    public static Assembly DataAssembly => typeof(EuropeCalendarData).Assembly;

    /// <summary>
    /// Creates an Austria rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateAustriaProvider() => CreateProvider(AustriaResourceName);

    /// <summary>
    /// Creates a Belgium rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateBelgiumProvider() => CreateProvider(BelgiumResourceName);

    /// <summary>
    /// Creates a Bulgaria rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateBulgariaProvider() => CreateProvider(BulgariaResourceName);

    /// <summary>
    /// Creates a Croatia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateCroatiaProvider() => CreateProvider(CroatiaResourceName);

    /// <summary>
    /// Creates a Cyprus rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateCyprusProvider() => CreateProvider(CyprusResourceName);

    /// <summary>
    /// Creates a Czechia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateCzechiaProvider() => CreateProvider(CzechiaResourceName);

    /// <summary>
    /// Creates a Denmark rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateDenmarkProvider() => CreateProvider(DenmarkResourceName);

    /// <summary>
    /// Creates an Estonia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateEstoniaProvider() => CreateProvider(EstoniaResourceName);

    /// <summary>
    /// Creates a Finland rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateFinlandProvider() => CreateProvider(FinlandResourceName);

    /// <summary>
    /// Creates a France rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateFranceProvider() => CreateProvider(FranceResourceName);

    /// <summary>
    /// Creates a Germany rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateGermanyProvider() => CreateProvider(GermanyResourceName);

    /// <summary>
    /// Creates a Greece rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateGreeceProvider() => CreateProvider(GreeceResourceName);

    /// <summary>
    /// Creates a Hungary rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateHungaryProvider() => CreateProvider(HungaryResourceName);

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
    /// Creates a Latvia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateLatviaProvider() => CreateProvider(LatviaResourceName);

    /// <summary>
    /// Creates a Lithuania rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateLithuaniaProvider() => CreateProvider(LithuaniaResourceName);

    /// <summary>
    /// Creates a Luxembourg rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateLuxembourgProvider() => CreateProvider(LuxembourgResourceName);

    /// <summary>
    /// Creates a Malta rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateMaltaProvider() => CreateProvider(MaltaResourceName);

    /// <summary>
    /// Creates a Netherlands rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateNetherlandsProvider() => CreateProvider(NetherlandsResourceName);

    /// <summary>
    /// Creates a Poland rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreatePolandProvider() => CreateProvider(PolandResourceName);

    /// <summary>
    /// Creates a Portugal rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreatePortugalProvider() => CreateProvider(PortugalResourceName);

    /// <summary>
    /// Creates a Romania rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateRomaniaProvider() => CreateProvider(RomaniaResourceName);

    /// <summary>
    /// Creates a Slovakia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSlovakiaProvider() => CreateProvider(SlovakiaResourceName);

    /// <summary>
    /// Creates a Slovenia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSloveniaProvider() => CreateProvider(SloveniaResourceName);

    /// <summary>
    /// Creates a Spain rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSpainProvider() => CreateProvider(SpainResourceName);

    /// <summary>
    /// Creates a Sweden rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSwedenProvider() => CreateProvider(SwedenResourceName);

    /// <summary>
    /// Creates a United Kingdom rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateUnitedKingdomProvider() => CreateProvider(UnitedKingdomResourceName);

    /// <summary>
    /// Creates one provider per country in the Europe pack so the full set can be passed straight to the
    /// <see cref="NotableDateService" /> constructor's <c>ruleProviders</c> parameter.
    /// </summary>
    /// <returns>An ordered enumeration of providers — one per country in the pack.</returns>
    public static IEnumerable<INotableDateRuleProvider> CreateProviders()
    {
        yield return CreateAustriaProvider();
        yield return CreateBelgiumProvider();
        yield return CreateBulgariaProvider();
        yield return CreateCroatiaProvider();
        yield return CreateCyprusProvider();
        yield return CreateCzechiaProvider();
        yield return CreateDenmarkProvider();
        yield return CreateEstoniaProvider();
        yield return CreateFinlandProvider();
        yield return CreateFranceProvider();
        yield return CreateGermanyProvider();
        yield return CreateGreeceProvider();
        yield return CreateHungaryProvider();
        yield return CreateIrelandProvider();
        yield return CreateItalyProvider();
        yield return CreateLatviaProvider();
        yield return CreateLithuaniaProvider();
        yield return CreateLuxembourgProvider();
        yield return CreateMaltaProvider();
        yield return CreateNetherlandsProvider();
        yield return CreatePolandProvider();
        yield return CreatePortugalProvider();
        yield return CreateRomaniaProvider();
        yield return CreateSlovakiaProvider();
        yield return CreateSloveniaProvider();
        yield return CreateSpainProvider();
        yield return CreateSwedenProvider();
        yield return CreateUnitedKingdomProvider();
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
