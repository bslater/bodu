// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedRangeCoverageTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies <see cref="LoadedRangeCoverage" />, which keeps the final days of each download for good and its open days
/// until the refresh interval has passed.
/// </summary>
[TestClass]
public sealed partial class LoadedRangeCoverageTests
{
    /// <summary>The instant the downloads in these tests were obtained: 09:00 UTC on 4 January 2023.</summary>
    private static readonly DateTimeOffset s_fetchedAt = new(2023, 1, 4, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The refresh interval the downloads in these tests are recorded with.</summary>
    private static readonly TimeSpan s_refresh = TimeSpan.FromHours(12);
}
