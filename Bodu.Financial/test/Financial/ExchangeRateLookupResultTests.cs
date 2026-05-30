// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateLookupResultTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

[TestClass]
public partial class ExchangeRateLookupResultTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupResult.IsExactDate" /> is <see langword="true" /> when the offset is
    /// zero days.
    /// </summary>
    [TestMethod]
    public void IsExactDate_WhenOffsetIsZero_ShouldReturnTrue()
    {
        DateOnly date = new(2024, 1, 3);
        ExchangeRate rate = new("USD", "AUD", date, 1.5m, "RBA");
        ExchangeRateLookupResult result = new(rate, date, ExchangeRateDateResolution.Exact, 0);

        Assert.IsTrue(result.IsExactDate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateLookupResult.IsExactDate" /> is <see langword="false" /> when the offset is
    /// non-zero.
    /// </summary>
    [TestMethod]
    public void IsExactDate_WhenOffsetIsNonZero_ShouldReturnFalse()
    {
        DateOnly requested = new(2024, 1, 5);
        DateOnly resolved = new(2024, 1, 3);
        ExchangeRate rate = new("USD", "AUD", resolved, 1.5m, "RBA");
        ExchangeRateLookupResult result = new(rate, requested, ExchangeRateDateResolution.PreviousOnOrBefore, 2);

        Assert.IsFalse(result.IsExactDate);
    }

    /// <summary>
    /// Verifies that records with identical components compare equal via the generated record-struct equality.
    /// </summary>
    [TestMethod]
    public void Equality_WhenComponentsMatch_ShouldReportEqual()
    {
        DateOnly date = new(2024, 1, 3);
        ExchangeRate rate = new("USD", "AUD", date, 1.5m, "RBA");
        ExchangeRateLookupResult a = new(rate, date, ExchangeRateDateResolution.Exact, 0);
        ExchangeRateLookupResult b = new(rate, date, ExchangeRateDateResolution.Exact, 0);

        Assert.AreEqual(a, b);
    }
}
