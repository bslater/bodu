// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DefaultNotableDateAlgorithms.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Algorithms;

/// <summary>
/// Exposes the engine's built-in date-calculation algorithms as a single pre-seeded
/// <see cref="INotableDateAlgorithmRegistry" />, so the built-in keys and a caller's custom keys resolve through one
/// uniform mechanism.
/// </summary>
/// <remarks>
/// <para>
/// Each built-in is registered as a <see cref="DelegateNotableDateAlgorithm" /> that captures the parameters its
/// calculator requires — for example the time-zone offset that dates an equinox holiday, or the festival key shared by
/// the Hindu lunisolar family — behind the parameterless <see cref="INotableDateAlgorithm.Calculate(int)" /> surface.
/// The registry is immutable after construction and safe to share across threads.
/// </para>
/// </remarks>
internal static class DefaultNotableDateAlgorithms
{
    /// <summary>
    /// The UTC offset, in hours, of Japan Standard Time, used to date the Japanese equinox holidays.
    /// </summary>
    private const double JapanStandardTimeOffset = 9.0;

    /// <summary>
    /// The UTC offset, in hours, of China Standard Time, used to date Qingming.
    /// </summary>
    private const double ChinaStandardTimeOffset = 8.0;

    /// <summary>
    /// The pre-seeded registry of every built-in algorithm, keyed by algorithm key.
    /// </summary>
    private static readonly INotableDateAlgorithmRegistry s_registry = Build();

    /// <summary>
    /// Gets the pre-seeded registry of every built-in algorithm.
    /// </summary>
    /// <returns>The built-in algorithm registry.</returns>
    public static INotableDateAlgorithmRegistry Registry =>
        s_registry;

    /// <summary>
    /// Builds the registry, registering every built-in algorithm under its key.
    /// </summary>
    /// <returns>The populated registry.</returns>
    private static INotableDateAlgorithmRegistry Build()
    {
        NotableDateAlgorithmRegistry registry = new();

        registry.Register(AlgorithmDateStrategy.WesternEasterKey, new DelegateNotableDateAlgorithm(EasterCalculator.Western));
        registry.Register(AlgorithmDateStrategy.OrthodoxEasterKey, new DelegateNotableDateAlgorithm(EasterCalculator.Orthodox));

        registry.Register("vernal-equinox", new DelegateNotableDateAlgorithm(year => SolarTermCalculator.VernalEquinox(year, 0.0)));
        registry.Register("autumnal-equinox", new DelegateNotableDateAlgorithm(year => SolarTermCalculator.AutumnalEquinox(year, 0.0)));
        registry.Register("jp-vernal-equinox", new DelegateNotableDateAlgorithm(year => SolarTermCalculator.VernalEquinox(year, JapanStandardTimeOffset)));
        registry.Register("jp-autumnal-equinox", new DelegateNotableDateAlgorithm(year => SolarTermCalculator.AutumnalEquinox(year, JapanStandardTimeOffset)));
        registry.Register("qingming", new DelegateNotableDateAlgorithm(year => SolarTermCalculator.Qingming(year, ChinaStandardTimeOffset)));

        registry.Register("vesak", new DelegateNotableDateAlgorithm(year => LunarPhaseCalculator.FullMoonOnOrAfter(new DateOnly(year, 5, 1))));
        registry.Register("losar", new DelegateNotableDateAlgorithm(TibetanLosarCalculator.Losar));
        registry.Register("matariki", new DelegateNotableDateAlgorithm(MatarikiCalendar.Resolve));

        foreach (var festivalKey in HinduLunarCalculator.FestivalKeys)
        {
            // Capture the key in a local so the delegate closes over this iteration's value.
            var key = festivalKey;
            registry.Register(key, new DelegateNotableDateAlgorithm(year => HinduLunarCalculator.Resolve(key, year)));
        }

        return registry;
    }
}
