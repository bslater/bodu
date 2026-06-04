// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Globalization.Calendar.V2.Data;

/// <summary>
/// Provides access to the embedded Europe notable-date resource pack (United Kingdom, France, Germany), migrated to the
/// v2 cookbook schema.
/// </summary>
/// <remarks>
/// <para>
/// Each supported country is a self-contained embedded resource. A territory may be a country code (<c>GB</c>) or a
/// subdivision (<c>GB-SCT</c>); the subdivision selects the same country resource, and the resolver filters by the full
/// territory at query time.
/// </para>
/// </remarks>
public static class EuropeCalendarData
{
    /// <summary>
    /// The manifest-resource-name prefix shared by the bundle's region resources.
    /// </summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.V2.Data.Resources.region-";

    /// <summary>
    /// Gets the country codes the Europe pack provides resources for.
    /// </summary>
    /// <returns>The supported ISO 3166-1 alpha-2 country codes.</returns>
    public static IReadOnlyList<string> SupportedCountries { get; } = new[]
    {
        "AT", "BE", "BG", "CY", "CZ", "DE", "DK", "EE", "ES", "FI", "FR", "GB", "GR", "HR",
        "HU", "IE", "IT", "LT", "LU", "LV", "MT", "NL", "PL", "PT", "RO", "SE", "SI", "SK",
    };

    /// <summary>
    /// Loads the notable-date resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>GB</c> or <c>GB-SCT</c>).</param>
    /// <returns>The loaded <see cref="NotableDateResource" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="territory" /> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException">The territory's country is not provided by this pack.</exception>
    public static NotableDateResource LoadResource(string territory)
    {
        ThrowHelper.ThrowIfNull(territory);

        string country = CountryOf(territory);
        string resourceName = ResourcePrefix + country.ToLowerInvariant() + ".xml";

        using Stream stream = typeof(EuropeCalendarData).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, "No Europe calendar resource for territory '{0}'.", territory),
                nameof(territory));

        return NotableDateResourceLoader.Load(stream, CommonNotableDateResources.Resolver);
    }

    /// <summary>
    /// Builds a resolver over the resource for the country owning the supplied territory.
    /// </summary>
    /// <param name="territory">A country code or subdivision (for example <c>GB</c> or <c>GB-SCT</c>).</param>
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
