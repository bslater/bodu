// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AlgorithmDateStrategy.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Calculates a notable date using a named, algorithm-backed computation such as Western or Orthodox Easter.
/// </summary>
/// <remarks>
/// <para>
/// The first cut recognizes the Easter keys only. An unrecognized key produces no occurrence; the loader reports it as
/// a validation error so a missing algorithm surfaces during loading rather than as a silently absent date.
/// </para>
/// </remarks>
public sealed class AlgorithmDateStrategy : IDateCalculationStrategy
{
    /// <summary>
    /// The algorithm key for Western (Gregorian) Easter Sunday.
    /// </summary>
    public const string WesternEasterKey = "western-easter";

    /// <summary>
    /// The algorithm key for Eastern Orthodox Easter Sunday.
    /// </summary>
    public const string OrthodoxEasterKey = "orthodox-easter";

    /// <summary>
    /// The set of algorithm keys the engine recognizes.
    /// </summary>
    private static readonly HashSet<string> s_knownKeys = new(StringComparer.Ordinal)
    {
        WesternEasterKey,
        OrthodoxEasterKey,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="AlgorithmDateStrategy" /> class.
    /// </summary>
    /// <param name="key">The algorithm key identifying the computation.</param>
    /// <exception cref="ArgumentNullException"><paramref name="key" /> is <see langword="null" />.</exception>
    public AlgorithmDateStrategy(string key)
    {
        ThrowHelper.ThrowIfNull(key);

        this.Key = key;
    }

    /// <summary>
    /// Gets the algorithm key identifying the computation.
    /// </summary>
    /// <returns>The algorithm key.</returns>
    public string Key { get; }

    /// <summary>
    /// Determines whether the supplied algorithm key is recognized by the engine.
    /// </summary>
    /// <param name="key">The algorithm key to test.</param>
    /// <returns><see langword="true" /> if the key is recognized; otherwise <see langword="false" />.</returns>
    public static bool IsKnownKey(string key) =>
        key is not null && s_knownKeys.Contains(key);

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (year < 1 || year > 9999)
            return null;

        return this.Key switch
        {
            WesternEasterKey => EasterCalculator.Western(year),
            OrthodoxEasterKey => EasterCalculator.Orthodox(year),
            _ => null,
        };
    }
}
