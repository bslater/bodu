// ---------------------------------------------------------------------------------------------------------------
// <copyright file="INotableDateAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Computes the date of a notable date for a Gregorian year from a closed-form or tabular algorithm. Implementations
/// are registered under a key and dispatched by the <see cref="AlgorithmDateStrategy" /> when a rule references that
/// key, allowing the algorithm catalogue to be extended beyond the built-in computations.
/// </summary>
/// <remarks>
/// <para>
/// Implementations must be deterministic, side-effect-free, and thread-safe, and should return <see langword="null" />
/// for a year outside their supported range rather than throwing.
/// </para>
/// </remarks>
public interface INotableDateAlgorithm
{
    /// <summary>
    /// Calculates the occurrence for the supplied Gregorian year.
    /// </summary>
    /// <param name="year">The Gregorian year to calculate against.</param>
    /// <returns>The occurrence date, or <see langword="null" /> when the year is unsupported.</returns>
    DateOnly? Calculate(int year);
}
