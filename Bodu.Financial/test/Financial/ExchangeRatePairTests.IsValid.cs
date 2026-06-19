// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRatePairTests.IsValid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class ExchangeRatePairTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRatePair.IsValid" /> reports <see langword="false" /> for the default value,
    /// because the default-struct path bypasses constructor validation and leaves both ISO codes null.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInstanceIsDefault_ShouldReturnFalse()
    {
        ExchangeRatePair pair = default;

        Assert.IsFalse(pair.IsValid);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRatePair.IsValid" /> reports <see langword="true" /> for a pair constructed
    /// through the validating constructor.
    /// </summary>
    [TestMethod]
    public void IsValid_WhenInstanceConstructedFromValidCodes_ShouldReturnTrue()
    {
        ExchangeRatePair pair = new(CurrencyCode.USD, CurrencyCode.AUD);

        Assert.IsTrue(pair.IsValid);
    }
}
