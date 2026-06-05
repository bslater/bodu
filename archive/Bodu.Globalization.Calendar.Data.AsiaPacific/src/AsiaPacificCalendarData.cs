// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsiaPacificCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Reflection;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides factory entry points for the Asia-Pacific region calendar data pack — embedded notable-date rules for
/// Australia, China, India, Japan, South Korea, Malaysia, New Zealand, and Singapore — so consumers can compose a
/// <see cref="NotableDateService" /> without knowing the underlying resource layout.
/// </summary>
/// <remarks>
/// <para>
/// Each region XML is embedded under the historical <c>Bodu.Globalization.Calendar.Resources.*</c> path so that
/// <c>&lt;UseFrom resource="./global-all.xml"&gt;</c> directives resolve against the main library's globals via the
/// provider's assembly chain.
/// </para>
/// <para>
/// Several rules in the Asia-Pacific pack (lunar, Hindu, Islamic, Buddhist) require custom
/// <see cref="INotableDateAlgorithm" /> implementations registered with the
/// <see cref="INotableDateAlgorithmRegistry" />. Without those registrations the affected dates resolve to no
/// occurrences without error; the remaining Western/Gregorian rules behave unaffected.
/// </para>
/// </remarks>
/// <example>
/// <code>
///<![CDATA[
/// var service = new NotableDateService(
///     ruleProviders: AsiaPacificCalendarData.CreateProviders(),
///     workingDaysOfWeek: WorkingDaysOfWeek.MondayToFriday);
///]]>
/// </code>
/// </example>
public static class AsiaPacificCalendarData
{
    /// <summary>
    /// The logical resource name for the Australia region payload.
    /// </summary>
    public const string AustraliaResourceName = "Bodu/Globalization/Calendar/Resources/region-au.xml";

    /// <summary>
    /// The logical resource name for the China region payload.
    /// </summary>
    public const string ChinaResourceName = "Bodu/Globalization/Calendar/Resources/region-cn.xml";

    /// <summary>
    /// The logical resource name for the India region payload.
    /// </summary>
    public const string IndiaResourceName = "Bodu/Globalization/Calendar/Resources/region-in.xml";

    /// <summary>
    /// The logical resource name for the Japan region payload.
    /// </summary>
    public const string JapanResourceName = "Bodu/Globalization/Calendar/Resources/region-jp.xml";

    /// <summary>
    /// The logical resource name for the South Korea region payload.
    /// </summary>
    public const string SouthKoreaResourceName = "Bodu/Globalization/Calendar/Resources/region-kr.xml";

    /// <summary>
    /// The logical resource name for the Malaysia region payload.
    /// </summary>
    public const string MalaysiaResourceName = "Bodu/Globalization/Calendar/Resources/region-my.xml";

    /// <summary>
    /// The logical resource name for the New Zealand region payload.
    /// </summary>
    public const string NewZealandResourceName = "Bodu/Globalization/Calendar/Resources/region-nz.xml";

    /// <summary>
    /// The logical resource name for the Singapore region payload.
    /// </summary>
    public const string SingaporeResourceName = "Bodu/Globalization/Calendar/Resources/region-sg.xml";

    /// <summary>
    /// Gets the assembly that hosts the pack's embedded XML resources. Exposed for advanced scenarios such as building
    /// a custom assembly chain.
    /// </summary>
    /// <returns>
    /// The <see cref="Assembly" /> in which the pack's region payloads are embedded. Never <see langword="null" />.
    /// </returns>
    public static Assembly DataAssembly => typeof(AsiaPacificCalendarData).Assembly;

    /// <summary>
    /// Creates an Australia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateAustraliaProvider() => CreateProvider(AustraliaResourceName);

    /// <summary>
    /// Creates a China rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateChinaProvider() => CreateProvider(ChinaResourceName);

    /// <summary>
    /// Creates an India rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateIndiaProvider() => CreateProvider(IndiaResourceName);

    /// <summary>
    /// Creates a Japan rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateJapanProvider() => CreateProvider(JapanResourceName);

    /// <summary>
    /// Creates a South Korea rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSouthKoreaProvider() => CreateProvider(SouthKoreaResourceName);

    /// <summary>
    /// Creates a Malaysia rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateMalaysiaProvider() => CreateProvider(MalaysiaResourceName);

    /// <summary>
    /// Creates a New Zealand rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateNewZealandProvider() => CreateProvider(NewZealandResourceName);

    /// <summary>
    /// Creates a Singapore rule provider.
    /// </summary>
    /// <returns>A configured rule provider.</returns>
    public static INotableDateRuleProvider CreateSingaporeProvider() => CreateProvider(SingaporeResourceName);

    /// <summary>
    /// Creates one provider per country in the Asia-Pacific pack so the full set can be passed straight to the
    /// <see cref="NotableDateService" /> constructor's <c>ruleProviders</c> parameter.
    /// </summary>
    /// <returns>An ordered enumeration of providers — one per country in the pack.</returns>
    public static IEnumerable<INotableDateRuleProvider> CreateProviders()
    {
        yield return CreateAustraliaProvider();
        yield return CreateChinaProvider();
        yield return CreateIndiaProvider();
        yield return CreateJapanProvider();
        yield return CreateSouthKoreaProvider();
        yield return CreateMalaysiaProvider();
        yield return CreateNewZealandProvider();
        yield return CreateSingaporeProvider();
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
