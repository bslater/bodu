// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedExchangeRateCacheOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedExchangeRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that validation rejects options whose provider is blank.
    /// </summary>
    [TestMethod]
    public void Validate_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "  " };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => options.Validate(),
            "Provider");
    }

    /// <summary>
    /// Verifies that validation rejects a key prefix that is supplied but consists only of white space.
    /// </summary>
    [TestMethod]
    public void Validate_WhenKeyPrefixIsWhiteSpace_ShouldThrowArgumentException()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "RBA", KeyPrefix = "   " };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () => options.Validate(),
            "KeyPrefix");
    }

    /// <summary>
    /// Verifies that validation accepts options without a key prefix.
    /// </summary>
    [TestMethod]
    public void Validate_WhenNoKeyPrefixSupplied_ShouldNotThrow()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "RBA" };

        options.Validate();
    }

    /// <summary>
    /// Verifies that validation accepts options with a non-blank key prefix.
    /// </summary>
    [TestMethod]
    public void Validate_WhenKeyPrefixSupplied_ShouldNotThrow()
    {
        var options = new DistributedExchangeRateCacheOptions { Provider = "RBA", KeyPrefix = "fx:" };

        options.Validate();
    }
}
