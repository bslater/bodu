// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesTests.WithRate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class ExchangeRateSeriesTests
{
    /// <summary>
    /// Verifies that <c>WithoutRate</c> returns the same instance when the requested date is not present, avoiding
    /// an unnecessary builder snapshot.
    /// </summary>
    [TestMethod]
    public void WithoutRate_WhenDateNotPresent_ShouldReturnSameInstance()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleRates());

        ExchangeRateSeries result = series.WithoutRate(new DateOnly(2099, 1, 1));

        Assert.AreSame(series, result);
    }

    /// <summary>
    /// Verifies that <c>WithoutRate</c> still produces a new instance when the requested date is removed.
    /// </summary>
    [TestMethod]
    public void WithoutRate_WhenDatePresent_ShouldReturnNewInstanceWithoutDate()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleRates());

        ExchangeRateSeries result = series.WithoutRate(new DateOnly(2024, 1, 3));

        Assert.AreNotSame(series, result);
        Assert.AreEqual(series.Count - 1, result.Count);
        Assert.IsFalse(result.TryGetRate(new DateOnly(2024, 1, 3), ExchangeRateLookupOptions.Exact, out _, out _));
    }

    /// <summary>
    /// Verifies that <c>WithRate</c> returns the same instance when the requested observation is already present
    /// with the supplied rate.
    /// </summary>
    [TestMethod]
    public void WithRate_WhenSameDateAndRateAlreadyPresent_ShouldReturnSameInstance()
    {
        ExchangeRateSeries series = new(s_usdAud, "RBA", SampleRates());

        ExchangeRateSeries result = series.WithRate(new DateOnly(2024, 1, 3), 1.51m);

        Assert.AreSame(series, result);
    }
}
