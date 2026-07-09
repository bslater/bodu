// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Calculates a notable date using a named, algorithm-backed computation such as Easter, an equinox, a lunar-phase
/// festival, or a gazetted date table.
/// </summary>
/// <remarks>
/// <para>
/// An unrecognized key produces no occurrence; the loader reports it as a validation error so a missing algorithm
/// surfaces during loading rather than as a silently absent date. Astronomical results are computed in the local time
/// zone appropriate to the observance (for example Japan Standard Time for the Japanese equinox holidays).
/// </para>
/// </remarks>
/// <seealso cref="IDateCalculationStrategy" /> <seealso cref="INotableDateAlgorithm" />
/// <seealso href="../guides/calendar/algorithms.html">Date calculation algorithms (guide)</seealso>
public sealed class AlgorithmDateStrategy
    : IDateCalculationStrategy
{
    /// <summary>The algorithm key for Western (Gregorian) Easter Sunday.</summary>
    public const string WesternEasterKey = "western-easter";

    /// <summary>The algorithm key for Eastern Orthodox Easter Sunday.</summary>
    public const string OrthodoxEasterKey = "orthodox-easter";

    /// <summary>
    /// Initializes a new instance of the <see cref="AlgorithmDateStrategy" /> class.
    /// </summary>
    /// <param name="key">The algorithm key identifying the computation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public AlgorithmDateStrategy(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        Key = key;
    }

    /// <summary>
    /// Gets the algorithm key identifying the computation.
    /// </summary>
    /// <value>The algorithm key.</value>
    public string Key { get; }

    /// <summary>
    /// Determines whether the supplied algorithm key is recognized by a built-in algorithm.
    /// </summary>
    /// <param name="key">The algorithm key to test.</param>
    /// <returns>
    /// <see langword="true" /> if a built-in algorithm is registered for the key; otherwise <see langword="false" />.
    /// </returns>
    public static bool IsKnownKey(string key) =>
        key is not null && DefaultNotableDateAlgorithms.Registry.Contains(key);

    /// <inheritdoc />
    /// <remarks>
    /// A key registered in the context's custom registry takes precedence over a built-in registration of the same key,
    /// so a document may override a built-in algorithm; an unrecognized key produces no occurrence.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="context" /> is <see langword="null" />.</exception>
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        ThrowHelper.ThrowIfNull(context);

        if (year is < 1 or > 9999)
            return null;

        if (context.Algorithms is INotableDateAlgorithmRegistry custom
            && custom.TryGet(Key, out INotableDateAlgorithm? customAlgorithm)
            && customAlgorithm is not null)
        {
            return customAlgorithm.Calculate(year);
        }

        return DefaultNotableDateAlgorithms.Registry.TryGet(Key, out INotableDateAlgorithm? builtIn) && builtIn is not null
            ? builtIn.Calculate(year)
            : null;
    }
}
