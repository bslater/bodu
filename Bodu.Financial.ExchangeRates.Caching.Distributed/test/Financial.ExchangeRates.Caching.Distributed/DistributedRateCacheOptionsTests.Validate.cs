// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DistributedRateCacheOptionsTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates.Caching.Distributed;

public sealed partial class DistributedRateCacheOptionsTests
{
    /// <summary>
    /// Verifies that validation rejects options whose provider is blank.
    /// </summary>
    [TestMethod]
    public void Validate_WhenProviderIsBlank_ShouldThrowArgumentException()
    {
        var options = new DistributedRateCacheOptions { Provider = "  " };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            options.Validate,
            "Provider");
    }

    /// <summary>
    /// Verifies that validation rejects a key prefix that is supplied but consists only of white space.
    /// </summary>
    [TestMethod]
    public void Validate_WhenKeyPrefixIsWhiteSpace_ShouldThrowArgumentException()
    {
        var options = new DistributedRateCacheOptions { Provider = "RBA", KeyPrefix = "   " };

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            options.Validate,
            "KeyPrefix");
    }

    /// <summary>
    /// Verifies that validation accepts options without a key prefix.
    /// </summary>
    [TestMethod]
    public void Validate_WhenNoKeyPrefixSupplied_ShouldNotThrow()
    {
        var options = new DistributedRateCacheOptions { Provider = "RBA" };

        options.Validate();
    }

    /// <summary>
    /// Verifies that validation accepts options with a non-blank key prefix.
    /// </summary>
    [TestMethod]
    public void Validate_WhenKeyPrefixSupplied_ShouldNotThrow()
    {
        var options = new DistributedRateCacheOptions { Provider = "RBA", KeyPrefix = "fx:" };

        options.Validate();
    }
}
