// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MiddleEastCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides access to the embedded Middle East notable-date resource pack (the United Arab Emirates, Saudi Arabia,
/// Israel, Turkey, Qatar, and Jordan), built on the notable-date schema.
/// </summary>
/// <remarks>
/// <para>
/// Each supported country is a self-contained embedded resource. A territory may be a country code (<c>AE</c>) or a
/// subdivision; the subdivision selects the same country resource, and the resolver filters by the full territory at
/// query time. Several packs declare a Friday/Saturday working week so weekend semantics resolve correctly.
/// </para>
/// <para>
/// <strong>When to use.</strong> Call <see cref="CreateService(string)" /> for a ready-to-query
/// <see cref="NotableDateService" />, or <see cref="LoadResource(string)" /> when you need the underlying
/// <see cref="NotableDateResource" /> to compose with custom collaborators or providers.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Query United Arab Emirates public holidays for a year.
/// NotableDateService service = MiddleEastCalendarData.CreateService("AE");
/// DateRange year = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
/// IReadOnlyList<NotableDate> holidays = service.Resolve(year, "AE");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateService" /> <seealso cref="NotableDateResource" />
/// <seealso href="../guides/calendar/data-packs.html">Calendar data packs (guide)</seealso>
public static class MiddleEastCalendarData
{
    /// <summary>The manifest-resource-name prefix shared by the bundle's region resources.</summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.Resources.region-";

    /// <summary>
    /// Gets the country codes the Middle East pack provides resources for.
    /// </summary>
    /// <value>The supported ISO 3166-1 alpha-2 country codes.</value>
    public static IReadOnlyList<string> SupportedCountries { get; } = ["AE", "IL", "JO", "QA", "SA", "TR"];

    /// <summary>
    /// Loads the notable-date resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>AE</c>).</param>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The territory's country is not provided by this pack.</exception>
    public static NotableDateResource LoadResource(string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        string country = CountryOf(territory);
        string resourceName = ResourcePrefix + country.ToLowerInvariant() + ".xml";

        using Stream stream = typeof(MiddleEastCalendarData).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException(
                string.Format(CultureInfo.CurrentCulture, MiddleEastCalendarDataResourceStrings.Arg_Invalid_NoResourceForTerritory, territory),
                nameof(territory));

        return NotableDateResourceLoader.Load(stream, ResolveResource);
    }

    /// <summary>
    /// Resolves an imported resource name to its content. The pan-regional <c>middleeast-common</c> hub is served from
    /// this pack's embedded resources; every other name (the shared catalogues such as <c>global-islamic</c> and
    /// <c>global-jewish</c>, including those that <c>middleeast-common</c> itself imports) is delegated to
    /// <see cref="CommonNotableDateResources" />.
    /// </summary>
    /// <param name="resourceName">The imported resource name, without extension.</param>
    /// <returns>The resource XML, or <see langword="null" /> when no resource of that name is available.</returns>
    private static string? ResolveResource(string resourceName) =>
        string.Equals(resourceName, "middleeast-common", StringComparison.OrdinalIgnoreCase)
            ? s_middleEastCommon.Value
            : CommonNotableDateResources.Resolve(resourceName);

    /// <summary>The lazily-read XML content of the embedded <c>middleeast-common</c> hub resource.</summary>
    private static readonly Lazy<string?> s_middleEastCommon = new(static () =>
    {
        using Stream? stream = typeof(MiddleEastCalendarData).Assembly
            .GetManifestResourceStream("Bodu.Globalization.Calendar.Resources.middleeast-common.xml");
        if (stream is null)
            return null;

        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    });

    /// <summary>
    /// Builds a resolver over the resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>AE</c>).</param>
    /// <returns>A service over the country's resource.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The territory's country is not provided by this pack.</exception>
    public static NotableDateService CreateService(string territory) =>
        new(LoadResource(territory));

    /// <summary>
    /// Extracts the country code from a territory, returning the segment before any subdivision separator.
    /// </summary>
    /// <param name="territory">The territory code.</param>
    /// <returns>The uppercase country code.</returns>
    private static string CountryOf(string territory)
    {
        int separator = territory.IndexOf('-', StringComparison.Ordinal);
        string country = separator < 0 ? territory : territory[..separator];

        return country.ToUpperInvariant();
    }
}
