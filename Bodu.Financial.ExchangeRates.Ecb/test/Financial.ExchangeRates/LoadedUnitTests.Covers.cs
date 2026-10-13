// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedUnitTests.Covers.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class LoadedUnitTests
{
    /// <summary>
    /// Verifies that a request ending on a final day is served for good, long after the refresh interval.
    /// </summary>
    [TestMethod]
    public void Covers_WhenRequestEndsOnFinalDay_ShouldReturnTrueForGood()
    {
        LoadedUnit unit = LoadedUnit.Create(null, new DateOnly(2023, 1, 3), s_fetchedAt, s_refresh);

        Assert.IsTrue(unit.Covers(new DateOnly(2023, 1, 3), s_fetchedAt.AddYears(1)));
    }

    /// <summary>
    /// Verifies that a request reaching an open day is served until the refresh interval has passed, and not after.
    /// </summary>
    [TestMethod]
    public void Covers_WhenRequestReachesOpenDay_ShouldReturnTrueOnlyUntilExpiry()
    {
        LoadedUnit unit = LoadedUnit.Create(null, new DateOnly(2023, 1, 3), s_fetchedAt, s_refresh);

        Assert.IsTrue(unit.Covers(new DateOnly(2023, 1, 4), s_fetchedAt.AddHours(11)));
        Assert.IsFalse(unit.Covers(new DateOnly(2023, 1, 4), s_fetchedAt.AddHours(12)));
    }
}
