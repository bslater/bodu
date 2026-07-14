// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheOptionsTests.EntryExpirationMargin.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that the margin defaults to one hour, so server-side expiration is on by default.
    /// </summary>
    [TestMethod]
    public void EntryExpirationMargin_WhenDefault_ShouldBeOneHour()
    {
        var options = new DistributedRateCacheOptions { Provider = "Test" };

        Assert.AreEqual(TimeSpan.FromHours(1), options.EntryExpirationMargin);
        Assert.IsTrue(options.TryValidate(out string? error), error);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> margin (server-side expiration disabled) and a zero margin both
    /// validate.
    /// </summary>
    [TestMethod]
    public void Validate_WhenMarginNullOrZero_ShouldNotThrow()
    {
        new DistributedRateCacheOptions { Provider = "Test", EntryExpirationMargin = null }.Validate();
        new DistributedRateCacheOptions { Provider = "Test", EntryExpirationMargin = TimeSpan.Zero }.Validate();
    }

    /// <summary>
    /// Verifies that a negative margin is rejected with the offending parameter name, and that
    /// <see cref="DistributedRateCacheOptions.TryValidate" /> agrees.
    /// </summary>
    [TestMethod]
    public void Validate_WhenMarginNegative_ShouldThrowArgumentException()
    {
        var options = new DistributedRateCacheOptions { Provider = "Test", EntryExpirationMargin = TimeSpan.FromSeconds(-1) };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(DistributedRateCacheOptions.EntryExpirationMargin), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out string? error));
        Assert.IsNotNull(error);
    }
}
