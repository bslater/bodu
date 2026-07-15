// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateCacheRulesTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the storage-agnostic merge and selection rules of <see cref="RateCacheRules" />, including the
/// single-incoming-row fast path against the general dictionary merge.
/// </summary>
[TestClass]
public sealed partial class RateCacheRulesTests
{
    /// <summary>The fixed evaluation instant the rules are checked against.</summary>
    private static readonly DateTimeOffset Now = new(2026, 6, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>The freshness duration applied by every test.</summary>
    private static readonly TimeSpan Duration = TimeSpan.FromHours(24);

    /// <summary>
    /// Builds a valid, fresh cached row for a date.
    /// </summary>
    /// <param name="day">The day-of-month in June 2026 the row observes.</param>
    /// <param name="rate">The rate value.</param>
    /// <param name="ageHours">The row's age in hours relative to <see cref="Now" />.</param>
    /// <returns>The cached row.</returns>
    private static CachedRate Row(int day, decimal rate = 1.5m, int ageHours = 1) =>
        new(new DateOnly(2026, 6, day), rate, Now - TimeSpan.FromHours(ageHours));
}
