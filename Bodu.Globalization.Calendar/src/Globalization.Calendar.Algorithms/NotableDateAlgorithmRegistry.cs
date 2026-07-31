// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateAlgorithmRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// A mutable registry of custom <see cref="INotableDateAlgorithm" /> implementations keyed by algorithm key.
/// </summary>
/// <remarks>
/// <para>
/// Keys are matched ordinally and are case-sensitive, mirroring the engine's built-in algorithm keys. Registering a key
/// that is already present replaces the previous algorithm.
/// </para>
/// <para>
/// <strong>When to use.</strong> Populate a registry with custom <see cref="INotableDateAlgorithm" /> implementations,
/// then pass it to <see cref="NotableDateResourceLoader" /> (so a document may reference the keys during validation)
/// and to the <see cref="NotableDateService" /> (so they resolve at query time).
/// </para>
/// <para>
/// <strong>Thread safety.</strong> Lookups are lock-free and safe to run concurrently with registrations: each
/// <see cref="Register" /> publishes a new immutable snapshot atomically, so a reader observes either the state before
/// or after a registration, never a torn intermediate. Prefer populating the registry before sharing it with a live
/// service; registrations performed afterwards become visible to in-flight resolutions at snapshot granularity.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry()
///     .Register("contoso.march-equinox", new MarchEquinoxAlgorithm())
///     .Register("contoso.harvest-moon", new HarvestMoonAlgorithm());
///
/// NotableDateResource resource = NotableDateResourceLoader.Load(documentXml, _ => null, registry);
/// NotableDateService service = new(resource, new NotableDateServiceOptions { Algorithms = registry });
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateAlgorithm" /> <seealso cref="NotableDateService" />
/// <seealso href="../guides/calendar/algorithms.html">Date calculation algorithms (guide)</seealso>
public sealed class NotableDateAlgorithmRegistry
    : INotableDateAlgorithmRegistry
{
    /// <summary>Serializes writers so each registration builds its snapshot from the latest published state.</summary>
    private readonly object _gate = new();

    /// <summary>
    /// The current immutable snapshot of registered algorithms, replaced atomically on each registration so lookups
    /// never observe a partially mutated state.
    /// </summary>
    private volatile Dictionary<string, INotableDateAlgorithm> _algorithms = new(StringComparer.Ordinal);

    /// <summary>
    /// Registers an algorithm under a key, replacing any existing registration.
    /// </summary>
    /// <param name="key">The algorithm key the strategy references.</param>
    /// <param name="algorithm">The algorithm implementation.</param>
    /// <returns>The same registry, to allow chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="key" /> or <paramref name="algorithm" /> is <see langword="null" />.
    /// </exception>
    public NotableDateAlgorithmRegistry Register(string key, INotableDateAlgorithm algorithm)
    {
        ThrowHelper.ThrowIfNull(key);
        ThrowHelper.ThrowIfNull(algorithm);

        lock (_gate)
        {
            // Copy-on-write: registration is rare and the map is small, so cloning keeps every lookup lock-free.
            Dictionary<string, INotableDateAlgorithm> next = new(_algorithms, StringComparer.Ordinal)
            {
                [key] = algorithm,
            };
            _algorithms = next;
        }

        return this;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Contains(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        return _algorithms.ContainsKey(key);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGet(string key, out INotableDateAlgorithm? algorithm)
    {
        ThrowHelper.ThrowIfNull(key);

        bool found = _algorithms.TryGetValue(key, out INotableDateAlgorithm? value);
        algorithm = value;
        return found;
    }
}
