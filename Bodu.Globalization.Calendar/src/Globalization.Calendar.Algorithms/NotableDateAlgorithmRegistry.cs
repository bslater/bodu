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
/// </remarks>
public sealed class NotableDateAlgorithmRegistry : INotableDateAlgorithmRegistry
{
    /// <summary>
    /// The registered algorithms keyed by algorithm key.
    /// </summary>
    private readonly Dictionary<string, INotableDateAlgorithm> _algorithms = new(StringComparer.Ordinal);

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

        this._algorithms[key] = algorithm;
        return this;
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool Contains(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        return this._algorithms.ContainsKey(key);
    }

    /// <inheritdoc />
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public bool TryGet(string key, out INotableDateAlgorithm? algorithm)
    {
        ThrowHelper.ThrowIfNull(key);

        var found = this._algorithms.TryGetValue(key, out INotableDateAlgorithm? value);
        algorithm = value;
        return found;
    }
}
