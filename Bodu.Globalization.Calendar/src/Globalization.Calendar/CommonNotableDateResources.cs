// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CommonNotableDateResources.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
using System.Globalization;
using System.Text;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Provides the shared, reusable notable-date resources bundled with the library — the civil, Christian, and other
/// thematic catalogues that define common observances (New Year's Day, Easter, Christmas, and the like) once for
/// territory packs to import rather than redefine.
/// </summary>
/// <remarks>
/// <para>
/// A territory resource imports a common catalogue by name through an <c>Import</c> directive, then cherry-picks the
/// concepts it observes and supplies its own territory and adjustment overrides. The <see cref="Resolver" /> delegate
/// is the bridge: pass it to
/// <see cref="NotableDateResourceLoader.Load(System.IO.Stream, System.Func{string, string}, Microsoft.Extensions.Logging.ILogger)" />
/// so those imports resolve against the embedded catalogues.
/// </para>
/// <para>
/// Resource names are the bare catalogue identifiers (for example <c>global-core</c> or <c>christian-western</c>),
/// matching the file names without their extension. Lookups are case-insensitive and the resolved content is cached.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // Load a territory document whose imports reference the shared catalogues, resolving those
/// // imports against the bundled common resources.
/// NotableDateResource resource = NotableDateResourceLoader.Load(
///     documentXml,
///     CommonNotableDateResources.Resolver);
///
/// NotableDateService service = new(resource);
///
/// // Resolve a single catalogue's raw XML directly when needed.
/// string? christianWestern = CommonNotableDateResources.Resolve("christian-western");
///]]>
/// </code>
/// </example>
/// </remarks>
public static class CommonNotableDateResources
{
    /// <summary>The manifest-resource-name prefix shared by the embedded common catalogues.</summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.Resources.";

    /// <summary>The cache of resolved catalogue content, keyed by case-insensitive resource name.</summary>
    private static readonly ConcurrentDictionary<string, string?> s_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a resolver delegate that maps a common-catalogue name to its embedded XML content, suitable for passing to
    /// the import-aware <see cref="NotableDateResourceLoader" /> overloads.
    /// </summary>
    /// <value>
    /// A resolver over the embedded common catalogues that returns <see langword="null" /> for unknown names.
    /// </value>
    public static Func<string, string?> Resolver { get; } = Resolve;

    /// <summary>
    /// Resolves a common-catalogue name to its embedded XML content.
    /// </summary>
    /// <param name="resourceName">The catalogue name, without extension (for example <c>christian-western</c>).</param>
    /// <returns>The catalogue XML, or <see langword="null" /> when no catalogue of that name is bundled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="resourceName" /> is <see langword="null" />.</exception>
    public static string? Resolve(string resourceName)
    {
        ThrowHelper.ThrowIfNull(resourceName);

        return s_cache.GetOrAdd(resourceName, static name =>
        {
            using Stream? stream = typeof(CommonNotableDateResources).Assembly
                .GetManifestResourceStream(ResourcePrefix + name + ".xml");
            if (stream is null)
                return null;

            using StreamReader reader = new(stream, Encoding.UTF8);
            return reader.ReadToEnd();
        });
    }

    /// <summary>
    /// Resolves a bundled catalogue to its embedded XML content by its strongly-typed name.
    /// </summary>
    /// <param name="catalog">The catalogue to resolve.</param>
    /// <returns>
    /// The catalogue XML, or <see langword="null" /> when the catalogue is not bundled with this assembly.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="catalog" /> is not a defined <see cref="CommonNotableDateCatalog" /> value.
    /// </exception>
    public static string? Resolve(CommonNotableDateCatalog catalog)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(catalog);

        return Resolve(GetResourceName(catalog));
    }

    /// <summary>
    /// Loads and validates a bundled catalogue into a <see cref="NotableDateResource" /> by its strongly-typed name,
    /// resolving any nested imports against the other bundled catalogues.
    /// </summary>
    /// <param name="catalog">The catalogue to load.</param>
    /// <returns>The materialized <see cref="NotableDateResource" /> for the requested catalogue.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="catalog" /> is not a defined <see cref="CommonNotableDateCatalog" /> value.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// The catalogue's content is not bundled with this assembly.
    /// </exception>
    /// <remarks>
    /// This is the direct path from a strongly-typed name to a usable resource — for example
    /// <c>CommonNotableDateResources.Load(CommonNotableDateCatalog.GlobalAll)</c> materializes the entire bundled
    /// <c>global-all</c> catalogue ready to hand to a <see cref="NotableDateService" />.
    /// </remarks>
    public static NotableDateResource Load(CommonNotableDateCatalog catalog)
    {
        ThrowHelper.ThrowIfEnumValueIsUndefined(catalog);

        string name = GetResourceName(catalog);
        string? content = Resolve(name);
        if (content is null)
            throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Op_Invalid_CommonCatalogNotBundled, name));

        return NotableDateResourceLoader.Load(content, Resolver);
    }

    /// <summary>
    /// Gets the bare resource name (for example <c>christian-western</c>) for a strongly-typed catalogue, suitable for
    /// an <c>Import</c> directive or a direct <see cref="Resolve(string)" /> call.
    /// </summary>
    /// <param name="catalog">The catalogue whose resource name to return.</param>
    /// <returns>The bare, kebab-case resource name that identifies the catalogue.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// <paramref name="catalog" /> is not a defined <see cref="CommonNotableDateCatalog" /> value.
    /// </exception>
    public static string GetResourceName(CommonNotableDateCatalog catalog) =>
        catalog switch
        {
            CommonNotableDateCatalog.Catholic => "catholic",
            CommonNotableDateCatalog.ChristianAnglican => "christian-anglican",
            CommonNotableDateCatalog.ChristianOrientalOrthodox => "christian-oriental-orthodox",
            CommonNotableDateCatalog.ChristianOrthodox => "christian-orthodox",
            CommonNotableDateCatalog.ChristianProtestant => "christian-protestant",
            CommonNotableDateCatalog.ChristianWestern => "christian-western",
            CommonNotableDateCatalog.DefaultMinimal => "default-minimal",
            CommonNotableDateCatalog.GlobalAll => "global-all",
            CommonNotableDateCatalog.GlobalAnchors => "global-anchors",
            CommonNotableDateCatalog.GlobalAnimals => "global-animals",
            CommonNotableDateCatalog.GlobalBahai => "global-bahai",
            CommonNotableDateCatalog.GlobalBuddhist => "global-buddhist",
            CommonNotableDateCatalog.GlobalCore => "global-core",
            CommonNotableDateCatalog.GlobalCultural => "global-cultural",
            CommonNotableDateCatalog.GlobalEducation => "global-education",
            CommonNotableDateCatalog.GlobalEnvironment => "global-environment",
            CommonNotableDateCatalog.GlobalFamilySocial => "global-family-social",
            CommonNotableDateCatalog.GlobalFamily => "global-family",
            CommonNotableDateCatalog.GlobalFood => "global-food",
            CommonNotableDateCatalog.GlobalHealth => "global-health",
            CommonNotableDateCatalog.GlobalHindu => "global-hindu",
            CommonNotableDateCatalog.GlobalIslamicUmmAlQura => "global-islamic-umm-al-qura",
            CommonNotableDateCatalog.GlobalIslamic => "global-islamic",
            CommonNotableDateCatalog.GlobalJain => "global-jain",
            CommonNotableDateCatalog.GlobalJewish => "global-jewish",
            CommonNotableDateCatalog.GlobalLunar => "global-lunar",
            CommonNotableDateCatalog.GlobalMultidayNormalization => "global-multiday-normalization",
            CommonNotableDateCatalog.GlobalPersian => "global-persian",
            CommonNotableDateCatalog.GlobalRemembrance => "global-remembrance",
            CommonNotableDateCatalog.GlobalScience => "global-science",
            CommonNotableDateCatalog.GlobalSikh => "global-sikh",
            CommonNotableDateCatalog.GlobalSocial => "global-social",
            CommonNotableDateCatalog.GlobalUn => "global-un",
            CommonNotableDateCatalog.GlobalZoroastrian => "global-zoroastrian",
            _ => throw new ArgumentOutOfRangeException(nameof(catalog)),
        };
}
