// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EuropeCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Calendar.Data;

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
/// <para>
/// <strong>When to use.</strong> Call <see cref="CreateService(string)" /> for a ready-to-query
/// <see cref="NotableDateService" />, or <see cref="LoadResource(string)" /> when you need the underlying
/// <see cref="NotableDateResource" /> to compose with custom collaborators or providers.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Scotland observes a different bank-holiday set from the rest of Great Britain.
/// NotableDateService service = EuropeCalendarData.CreateService("GB-SCT");
/// DateRange year = new(new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31));
/// IReadOnlyList<NotableDate> holidays = service.Resolve(year, "GB-SCT");
///]]>
/// </code>
/// </example>
/// <seealso cref="NotableDateService" />
/// <seealso cref="NotableDateResource" />
public static class EuropeCalendarData
{
    /// <summary>
    /// The manifest-resource-name prefix shared by the bundle's region resources.
    /// </summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.Data.Resources.region-";

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

        return NotableDateResourceLoader.Load(stream, ResolveResource);
    }

    /// <summary>
    /// Resolves an imported resource name to its content. The pan-European <c>europe-common</c> hub is served from
    /// this pack's embedded resources; every other name (the shared catalogues such as <c>christian-western</c> and
    /// <c>global-core</c>, including those that <c>europe-common</c> itself imports) is delegated to
    /// <see cref="CommonNotableDateResources" />.
    /// </summary>
    /// <param name="resourceName">The imported resource name, without extension.</param>
    /// <returns>The resource XML, or <see langword="null" /> when no resource of that name is available.</returns>
    private static string? ResolveResource(string resourceName) =>
        string.Equals(resourceName, "europe-common", StringComparison.OrdinalIgnoreCase)
            ? s_europeCommon.Value
            : CommonNotableDateResources.Resolve(resourceName);

    /// <summary>
    /// The lazily-read XML content of the embedded <c>europe-common</c> hub resource.
    /// </summary>
    private static readonly Lazy<string?> s_europeCommon = new(static () =>
    {
        using Stream? stream = typeof(EuropeCalendarData).Assembly
            .GetManifestResourceStream("Bodu.Globalization.Calendar.Data.Resources.europe-common.xml");
        if (stream is null)
            return null;

        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    });

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
