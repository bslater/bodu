// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmericasCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Reflection;

namespace Bodu.Globalization.Calendar.Data;

/// <summary>
/// Provides access to the embedded Americas notable-date resource pack (Canada and the United States), migrated to the
/// v2 cookbook schema.
/// </summary>
/// <remarks>
/// <para>
/// Each supported country is a self-contained embedded resource. A territory may be a country code (<c>US</c>) or a
/// subdivision (<c>CA-ON</c>); the subdivision selects the same country resource, and the resolver filters by the full
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
/// // Query United States federal holidays for a year.
/// NotableDateService service = AmericasCalendarData.CreateService("US");
/// DateRange year = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
/// IReadOnlyList<NotableDate> holidays = service.Resolve(year, "US");
///
/// // A subdivision sees its province's rules layered over the country's.
/// NotableDateService ontario = AmericasCalendarData.CreateService("CA-ON");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateService" />
/// <seealso cref="NotableDateResource" />
/// <seealso href="../guides/calendar/data-packs.html">Calendar data packs (guide)</seealso>
public static class AmericasCalendarData
{
    /// <summary>
    /// The manifest-resource-name prefix shared by the bundle's region resources.
    /// </summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.Data.Resources.region-";

    /// <summary>
    /// Gets the country codes the Americas pack provides resources for.
    /// </summary>
    /// <returns>The supported ISO 3166-1 alpha-2 country codes.</returns>
    public static IReadOnlyList<string> SupportedCountries { get; } = new[] { "CA", "US" };

    /// <summary>
    /// Loads the notable-date resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>US</c> or <c>CA-ON</c>).</param>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The territory's country is not provided by this pack.</exception>
    public static NotableDateResource LoadResource(string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        string country = CountryOf(territory);
        string resourceName = ResourcePrefix + country.ToLowerInvariant() + ".xml";

        using Stream stream = typeof(AmericasCalendarData).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "No Americas calendar resource for territory '{0}'.", territory),
                nameof(territory));

        return NotableDateResourceLoader.Load(stream, CommonNotableDateResources.Resolver);
    }

    /// <summary>
    /// Builds a resolver over the resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>US</c> or <c>CA-ON</c>).</param>
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
