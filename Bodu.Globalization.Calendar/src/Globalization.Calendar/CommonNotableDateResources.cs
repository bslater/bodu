// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CommonNotableDateResources.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections.Concurrent;
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
/// is the bridge: pass it to <see cref="NotableDateResourceLoader.Load(System.IO.Stream, System.Func{string, string})" />
/// so those imports resolve against the embedded catalogues.
/// </para>
/// <para>
/// Resource names are the bare catalogue identifiers (for example <c>global-core</c> or <c>christian-western</c>),
/// matching the file names without their extension. Lookups are case-insensitive and the resolved content is cached.
/// </para>
/// </remarks>
public static class CommonNotableDateResources
{
    /// <summary>
    /// The manifest-resource-name prefix shared by the embedded common catalogues.
    /// </summary>
    private const string ResourcePrefix = "Bodu.Globalization.Calendar.Resources.";

    /// <summary>
    /// The cache of resolved catalogue content, keyed by case-insensitive resource name.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string?> s_cache = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets a resolver delegate that maps a common-catalogue name to its embedded XML content, suitable for passing to
    /// the import-aware <see cref="NotableDateResourceLoader" /> overloads.
    /// </summary>
    /// <returns>A resolver over the embedded common catalogues that returns <see langword="null" /> for unknown names.</returns>
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
}
