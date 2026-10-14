// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateAlgorithmRegistry.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Provides lookup of custom <see cref="INotableDateAlgorithm" /> implementations by key, supplementing the engine's
/// built-in algorithm catalog.
/// </summary>
/// <remarks>
/// <para>
/// <see cref="AlgorithmDateStrategy" /> consults the custom registry first and falls back to the built-in catalog, so a
/// key registered here overrides a built-in algorithm of the same name.
/// </para>
/// </remarks>
public interface INotableDateAlgorithmRegistry
{
    /// <summary>
    /// Determines whether an algorithm is registered under the supplied key.
    /// </summary>
    /// <param name="key">The algorithm key.</param>
    /// <returns><see langword="true" /> if an algorithm is registered; otherwise <see langword="false" />.</returns>
    bool Contains(string key);

    /// <summary>
    /// Attempts to retrieve the algorithm registered under the supplied key.
    /// </summary>
    /// <param name="key">The algorithm key.</param>
    /// <param name="algorithm">
    /// When this method returns, the registered algorithm, or <see langword="null" /> when none.
    /// </param>
    /// <returns><see langword="true" /> if an algorithm was found; otherwise <see langword="false" />.</returns>
    bool TryGet(string key, out INotableDateAlgorithm? algorithm);
}
