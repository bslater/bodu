// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedUnitTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Verifies <see cref="LoadedUnit" />, the record of a loaded feed, era or month that decides when it must be downloaded
/// again.
/// </summary>
[TestClass]
public sealed partial class LoadedUnitTests
{
    /// <summary>The instant the units in these tests were obtained: 09:00 UTC on 4 January 2023.</summary>
    private static readonly DateTimeOffset s_fetchedAt = new(2023, 1, 4, 9, 0, 0, TimeSpan.Zero);

    /// <summary>The refresh interval the units in these tests are recorded with.</summary>
    private static readonly TimeSpan s_refresh = TimeSpan.FromHours(12);
}
