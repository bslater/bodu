// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AmericasCalendarData.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides access to the embedded Americas notable-date resource pack (Canada and the United States), built on the
/// notable-date schema.
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
/// <seealso cref="NotableDateService" /> <seealso cref="NotableDateResource" />
/// <seealso href="../guides/calendar/data-packs.html">Calendar data packs (guide)</seealso>
public static class AmericasCalendarData
{
    /// <summary>
    /// The manifest-resource-name prefix shared by the bundle's region resources.
    /// </summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.Resources.region-";

    /// <summary>
    /// Gets the country codes the Americas pack provides resources for.
    /// </summary>
    /// <returns>The supported ISO 3166-1 alpha-2 country codes.</returns>
    public static IReadOnlyList<string> SupportedCountries { get; } =
    [
        "AR", "BR", "CA", "CL", "CO", "MX", "PE", "US",
    ];

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

        var country = CountryOf(territory);
        var resourceName = ResourcePrefix + country.ToLowerInvariant() + ".xml";

        using Stream stream = typeof(AmericasCalendarData).Assembly.GetManifestResourceStream(resourceName)
            ?? throw new ArgumentException(
                string.Format(CultureInfo.InvariantCulture, AmericasCalendarDataResourceStrings.Arg_Invalid_NoResourceForTerritory, territory),
                nameof(territory));

        return NotableDateResourceLoader.Load(stream, ResolveResource);
    }

    /// <summary>
    /// Resolves an imported resource name to its content. The pan-American <c>americas-common</c> hub is served from
    /// this pack's embedded resources; every other name (the shared catalogues such as <c>christian-western</c> and
    /// <c>global-core</c>, including those that <c>americas-common</c> itself imports) is delegated to
    /// <see cref="CommonNotableDateResources" />.
    /// </summary>
    /// <param name="resourceName">The imported resource name, without extension.</param>
    /// <returns>The resource XML, or <see langword="null" /> when no resource of that name is available.</returns>
    private static string? ResolveResource(string resourceName) =>
        string.Equals(resourceName, "americas-common", StringComparison.OrdinalIgnoreCase)
            ? s_americasCommon.Value
            : CommonNotableDateResources.Resolve(resourceName);

    /// <summary>
    /// The lazily-read XML content of the embedded <c>americas-common</c> hub resource.
    /// </summary>
    private static readonly Lazy<string?> s_americasCommon = new(static () =>
    {
        using Stream? stream = typeof(AmericasCalendarData).Assembly
            .GetManifestResourceStream("Bodu.Globalization.Calendar.Resources.americas-common.xml");
        if (stream is null)
            return null;

        using StreamReader reader = new(stream, Encoding.UTF8);
        return reader.ReadToEnd();
    });

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
        var separator = territory.IndexOf('-', StringComparison.Ordinal);
        var country = separator < 0 ? territory : territory[..separator];

        return country.ToUpperInvariant();
    }
}
