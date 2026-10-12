// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderOptionsTests.ObservationIngestedLogLevel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="RbaRateProviderOptions.ObservationIngestedLogLevel" /> defaults to
    /// <see cref="LogLevel.Trace" />, so the per-observation event stays out of ordinary logs.
    /// </summary>
    [TestMethod]
    public void ObservationIngestedLogLevel_WhenDefault_ShouldBeTrace()
    {
        var options = new RbaRateProviderOptions();

        Assert.AreEqual(LogLevel.Trace, options.ObservationIngestedLogLevel);
    }
}
