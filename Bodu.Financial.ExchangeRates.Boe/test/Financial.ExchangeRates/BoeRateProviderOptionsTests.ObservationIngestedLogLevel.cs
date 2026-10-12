// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeRateProviderOptionsTests.ObservationIngestedLogLevel.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="BoeRateProviderOptions.ObservationIngestedLogLevel" /> defaults to
    /// <see cref="LogLevel.Trace" />, so the per-observation event stays out of ordinary logs.
    /// </summary>
    [TestMethod]
    public void ObservationIngestedLogLevel_WhenDefault_ShouldBeTrace()
    {
        var options = new BoeRateProviderOptions();

        Assert.AreEqual(LogLevel.Trace, options.ObservationIngestedLogLevel);
    }
}
