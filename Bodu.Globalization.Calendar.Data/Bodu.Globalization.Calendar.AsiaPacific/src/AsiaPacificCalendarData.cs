// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsiaPacificCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides access to the embedded Asia-Pacific notable-date resource pack (Australia, China, Japan, New Zealand),
/// migrated to the v2 cookbook schema.
/// </summary>
/// <remarks>
/// <para>
/// Each supported country is a self-contained embedded resource. A territory may be a country code (<c>AU</c>) or a
/// subdivision (<c>AU-WA</c>); the subdivision selects the same country resource, and the resolver filters by the full
/// territory at query time.
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
/// // Query public holidays for Western Australia.
/// NotableDateService service = AsiaPacificCalendarData.CreateService("AU-WA");
/// DateRange year = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
/// IReadOnlyList<NotableDate> holidays = service.Resolve(year, "AU-WA");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateService" />
/// <seealso cref="NotableDateResource" />
/// <seealso href="../guides/calendar/data-packs.html">Calendar data packs (guide)</seealso>
public static class AsiaPacificCalendarData
{
    /// <summary>
    /// The manifest-resource-name prefix shared by the bundle's region resources.
    /// </summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.Resources.region-";

    /// <summary>
    /// Gets the country codes the Asia-Pacific pack provides resources for.
    /// </summary>
    /// <returns>The supported ISO 3166-1 alpha-2 country codes.</returns>
    public static IReadOnlyList<string> SupportedCountries { get; } = new[] { "AU", "CN", "IN", "JP", "KR", "MY", "NZ", "SG" };

    /// <summary>
    /// Loads the notable-date resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>AU</c> or <c>AU-WA</c>).</param>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The territory's country is not provided by this pack.</exception>
    public static NotableDateResource LoadResource(string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        var country = CountryOf(territory);
        var resourceName = ResourcePrefix + country.ToLowerInvariant() + ".xml";

        using Stream stream = typeof(AsiaPacificCalendarData).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "No Asia-Pacific calendar resource for territory '{0}'.", territory),
                nameof(territory));

        return NotableDateResourceLoader.Load(stream, CommonNotableDateResources.Resolver);
    }

    /// <summary>
    /// Builds a resolver over the resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>AU</c> or <c>AU-WA</c>).</param>
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
        var separator = territory.IndexOf('-', StringComparison.Ordinal);
        var country = separator < 0 ? territory : territory[..separator];

        return country.ToUpperInvariant();
    }
}
