// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Verifies the defaults and validation rules of <see cref="CachingRateOptions" />.
/// </summary>
[TestClass]
public sealed partial class CachingRateOptionsTests
{
    /// <summary>
    /// Verifies the constructed defaults: a 24-hour default expiry, no jitter, the inverse probe not skipped, and a
    /// valid configuration overall.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaults_ShouldBeValidWithExpectedValues()
    {
        var options = new CachingRateOptions();

        Assert.AreEqual(TimeSpan.FromHours(24), options.DefaultExpiry);
        Assert.AreEqual(0d, options.ExpiryJitter);
        Assert.IsFalse(options.SkipInverseRangeProbeWhenDirectCovered);
        Assert.IsTrue(options.TryValidate(out string? error), error);
    }
}
