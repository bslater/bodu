// ---------------------------------------------------------------------------------------------------------------
// <copyright file="LoadedUnitTests.Create.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public sealed partial class LoadedUnitTests
{
    /// <summary>
    /// Verifies that a unit that ended before the day it was loaded is final everywhere and never expires.
    /// </summary>
    [TestMethod]
    public void Create_WhenUnitEndedBeforeTheDownload_ShouldNeverExpire()
    {
        LoadedUnit unit = LoadedUnit.Create(new DateOnly(2022, 12, 31), new DateOnly(2022, 12, 30), s_fetchedAt, s_refresh);

        Assert.AreEqual(DateOnly.MaxValue, unit.FinalThrough);
        Assert.AreEqual(DateTimeOffset.MaxValue, unit.ExpiresAt);
    }

    /// <summary>
    /// Verifies that an open-ended unit is final through the day before it was loaded and expires one refresh interval
    /// later.
    /// </summary>
    [TestMethod]
    public void Create_WhenUnitIsOpenEnded_ShouldBeFinalThroughYesterdayAndExpire()
    {
        LoadedUnit unit = LoadedUnit.Create(null, new DateOnly(2023, 1, 3), s_fetchedAt, s_refresh);

        Assert.AreEqual(new DateOnly(2023, 1, 3), unit.FinalThrough);
        Assert.AreEqual(s_fetchedAt + s_refresh, unit.ExpiresAt);
    }

    /// <summary>
    /// Verifies that a unit whose last day is still to come stays open, even though it ends.
    /// </summary>
    [TestMethod]
    public void Create_WhenUnitEndsLater_ShouldStayOpen()
    {
        LoadedUnit unit = LoadedUnit.Create(new DateOnly(2023, 1, 31), new DateOnly(2023, 1, 3), s_fetchedAt, s_refresh);

        Assert.AreEqual(new DateOnly(2023, 1, 3), unit.FinalThrough);
        Assert.AreEqual(s_fetchedAt + s_refresh, unit.ExpiresAt);
    }
}
