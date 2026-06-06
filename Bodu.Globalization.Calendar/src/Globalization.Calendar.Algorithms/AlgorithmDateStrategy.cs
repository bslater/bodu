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
/// <seealso cref="IDateCalculationStrategy" />
/// <seealso cref="INotableDateAlgorithm" />
/// <seealso href="../guides/calendar/algorithms.html">Date calculation algorithms (guide)</seealso>
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
    /// The UTC offset, in hours, of Japan Standard Time, used to date the Japanese equinox holidays.
    /// </summary>
    private const double JapanStandardTimeOffset = 9.0;

    /// <summary>
    /// The UTC offset, in hours, of China Standard Time, used to date Qingming.
    /// </summary>
    private const double ChinaStandardTimeOffset = 8.0;

    /// <summary>
    /// The set of algorithm keys the engine recognizes by a closed-form or tabular computation, excluding the Hindu
    /// festival keys handled by <see cref="HinduLunarCalculator" />.
    /// </summary>
    private static readonly HashSet<string> s_knownKeys = new(StringComparer.Ordinal)
    {
        WesternEasterKey,
        OrthodoxEasterKey,
        "vernal-equinox",
        "autumnal-equinox",
        "jp-vernal-equinox",
        "jp-autumnal-equinox",
        "qingming",
        "vesak",
        "asalha-puja",
        "losar",
        "matariki",
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
        key is not null && (s_knownKeys.Contains(key) || HinduLunarCalculator.IsFestivalKey(key));

    /// <inheritdoc />
    public DateOnly? Calculate(int year, StrategyResolutionContext context)
    {
        if (year < 1 || year > 9999)
            return null;

        return this.Key switch
        {
            WesternEasterKey => EasterCalculator.Western(year),
            OrthodoxEasterKey => EasterCalculator.Orthodox(year),
            "vernal-equinox" => SolarTermCalculator.VernalEquinox(year, 0.0),
            "autumnal-equinox" => SolarTermCalculator.AutumnalEquinox(year, 0.0),
            "jp-vernal-equinox" => SolarTermCalculator.VernalEquinox(year, JapanStandardTimeOffset),
            "jp-autumnal-equinox" => SolarTermCalculator.AutumnalEquinox(year, JapanStandardTimeOffset),
            "qingming" => SolarTermCalculator.Qingming(year, ChinaStandardTimeOffset),
            "vesak" => LunarPhaseCalculator.FullMoonOnOrAfter(new DateOnly(year, 5, 1)),
            "asalha-puja" => LunarPhaseCalculator.FullMoonOnOrAfter(new DateOnly(year, 6, 15)),
            "losar" => LunarPhaseCalculator.NewMoonOnOrAfter(new DateOnly(year, 1, 20)),
            "matariki" => MatarikiCalendar.Resolve(year),
            _ when HinduLunarCalculator.IsFestivalKey(this.Key) => HinduLunarCalculator.Resolve(this.Key, year),
            _ => context.Algorithms is INotableDateAlgorithmRegistry registry && registry.TryGet(this.Key, out INotableDateAlgorithm? algorithm) && algorithm is not null
                ? algorithm.Calculate(year)
                : null,
        };
    }
}
