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
/// <para>
/// <strong>When to implement.</strong> Provide a custom algorithm when a notable date follows a computation the
/// built-in strategies do not cover — for example an astronomical event or a bespoke ecclesiastical rule. Register the
/// implementation under a key in an <see cref="INotableDateAlgorithmRegistry" />, pass that registry to the loader and
/// the <see cref="NotableDateService" />, and reference the key from a rule's algorithm strategy.
/// </para>
/// </remarks>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// // A custom algorithm returning the March equinox (UTC) for the supplied year.
/// public sealed class MarchEquinoxAlgorithm : INotableDateAlgorithm
/// {
///     public DateOnly? Calculate(int year) =>
///         year is >= 1900 and <= 2100
///             ? DateOnly.FromDateTime(AstronomyTables.MarchEquinox(year))
///             : null; // unsupported year: return null rather than throw
/// }
///]]>
/// </code>
/// </example>
/// <seealso cref="INotableDateAlgorithmRegistry" />
/// <seealso cref="AlgorithmDateStrategy" />
public interface INotableDateAlgorithm
{
    /// <summary>
    /// Calculates the occurrence for the supplied Gregorian year.
    /// </summary>
    /// <param name="year">The Gregorian year to calculate against.</param>
    /// <returns>The occurrence date, or <see langword="null" /> when the year is unsupported.</returns>
    DateOnly? Calculate(int year);
}
