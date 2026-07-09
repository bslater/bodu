// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateOfTBaseTQuoteTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial.ExchangeRates;

public partial class ExchangeRateOfTBaseTQuoteTests
{

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentOutOfRangeException" /> when the supplied rate is zero
    /// or negative.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRateIsNotPositive_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new ExchangeRate<USD, AUD>(0m, SampleDate, SampleProvider);
        });
    }

    /// <summary>
    /// Verifies that the constructor throws <see cref="ArgumentException" /> when <c>provider</c> is white-space.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenProviderIsWhiteSpace_ShouldThrowArgumentException()
    {
        Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = new ExchangeRate<USD, AUD>(1.52m, SampleDate, "  ");
        });
    }
}
