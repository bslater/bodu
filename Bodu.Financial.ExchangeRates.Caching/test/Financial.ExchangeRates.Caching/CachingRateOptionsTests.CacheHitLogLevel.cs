// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateOptionsTests.CacheHitLogLevel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingRateOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="CachingRateOptions.CacheHitLogLevel" /> defaults to <see cref="LogLevel.Trace" />, so the
    /// per-lookup single-date cache hit event on the read hot path stays out of ordinary logs.
    /// </summary>
    [TestMethod]
    public void CacheHitLogLevel_WhenDefault_ShouldBeTrace()
    {
        var options = new CachingRateOptions();

        Assert.AreEqual(LogLevel.Trace, options.CacheHitLogLevel);
    }
}
