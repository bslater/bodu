// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeExchangeRateOptionsTests.TryValidate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class BoeExchangeRateOptionsTests
{
    /// <summary>
    /// Verifies that the defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        BoeExchangeRateOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a null endpoint is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenEndpointIsNull_ShouldReturnFalse()
    {
        BoeExchangeRateOptions options = new() { Endpoint = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an invalid endpoint surfaces through the aggregate validation.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenEndpointInvalid_ShouldReturnFalse()
    {
        BoeExchangeRateOptions options = new();
        options.Endpoint.QueryPath = "   ";

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an empty series catalogue is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenSeriesEmpty_ShouldReturnFalse()
    {
        BoeExchangeRateOptions options = new() { Series = [] };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a negative on-demand window radius is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenOnDemandWindowDaysNegative_ShouldReturnFalse()
    {
        BoeExchangeRateOptions options = new() { OnDemandWindowDays = -1 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive refresh interval is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenRefreshIntervalIsNegative_ShouldReturnFalse()
    {
        BoeExchangeRateOptions options = new() { RefreshInterval = TimeSpan.FromHours(-1) };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an undefined log level is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenLogLevelUndefined_ShouldReturnFalse()
    {
        BoeExchangeRateOptions options = new() { ObservationIngestedLogLevel = (LogLevel)999 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }
}
