// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbRateProviderOptionsTests.ObservationIngestedLogLevel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="EcbRateProviderOptions.ObservationIngestedLogLevel" /> defaults to
    /// <see cref="LogLevel.Trace" />, so the per-observation event stays out of ordinary logs.
    /// </summary>
    [TestMethod]
    public void ObservationIngestedLogLevel_WhenDefault_ShouldBeTrace()
    {
        var options = new EcbRateProviderOptions();

        Assert.AreEqual(LogLevel.Trace, options.ObservationIngestedLogLevel);
    }
}
