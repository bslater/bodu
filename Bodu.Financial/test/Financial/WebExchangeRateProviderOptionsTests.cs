// ---------------------------------------------------------------------------------------------------------------
// <copyright file="WebExchangeRateProviderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Financial;

/// <summary>
/// Verifies the shared validation behaviour of <see cref="WebExchangeRateProviderOptions" />, including delegation to
/// the source-specific <c>TryValidateCore</c> override.
/// </summary>
[TestClass]
public class WebExchangeRateProviderOptionsTests
{
    /// <summary>
    /// Verifies that the defaults validate successfully.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefault_ShouldReturnTrue()
    {
        TestWebExchangeRateProviderOptions options = new();

        bool valid = options.TryValidate(out string? error);

        Assert.IsTrue(valid);
        Assert.IsNull(error);
    }

    /// <summary>
    /// Verifies that a null base address is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenBaseAddressIsNull_ShouldReturnFalse()
    {
        TestWebExchangeRateProviderOptions options = new() { BaseAddress = null! };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive HTTP timeout is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenHttpTimeoutIsZero_ShouldReturnFalse()
    {
        TestWebExchangeRateProviderOptions options = new() { HttpTimeout = TimeSpan.Zero };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a non-positive default look-back window is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenDefaultLookbackIsZero_ShouldReturnFalse()
    {
        TestWebExchangeRateProviderOptions options = new() { DefaultLookback = TimeSpan.Zero };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that a null currency-alias map is rejected.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCurrencyAliasesIsNull_ShouldReturnFalse()
    {
        TestWebExchangeRateProviderOptions options = new() { CurrencyAliases = null! };

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
        TestWebExchangeRateProviderOptions options = new() { SynchronousNetworkFetchLogLevel = (LogLevel)999 };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that the shared validation delegates to the source-specific <c>TryValidateCore</c> when the shared
    /// invariants hold.
    /// </summary>
    [TestMethod]
    public void TryValidate_WhenCoreInvalid_ShouldReturnFalse()
    {
        TestWebExchangeRateProviderOptions options = new() { CoreValid = false };

        bool valid = options.TryValidate(out string? error);

        Assert.IsFalse(valid);
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that <see cref="WebExchangeRateProviderOptions.Validate" /> throws for invalid options.
    /// </summary>
    [TestMethod]
    public void Validate_WhenInvalid_ShouldThrowArgumentException()
    {
        TestWebExchangeRateProviderOptions options = new() { CoreValid = false };

        _ = Assert.ThrowsExactly<ArgumentException>(options.Validate);
    }
}
