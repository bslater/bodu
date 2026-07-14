// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CachingRateOptionsTests.ExpiryJitter.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

public sealed partial class CachingRateOptionsTests
{
    /// <summary>
    /// Verifies that jitter fractions within <c>[0, 1)</c> validate successfully.
    /// </summary>
    /// <param name="fraction">The jitter fraction to configure.</param>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(0.1)]
    [DataRow(0.999)]
    public void Validate_WhenExpiryJitterInRange_ShouldNotThrow(double fraction)
    {
        var options = new CachingRateOptions { ExpiryJitter = fraction };

        options.Validate();

        Assert.IsTrue(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that a jitter fraction outside <c>[0, 1)</c> — or not a number — is rejected with the offending
    /// parameter name, and that <see cref="CachingRateOptions.TryValidate" /> agrees.
    /// </summary>
    /// <param name="fraction">The invalid jitter fraction.</param>
    [TestMethod]
    [DataRow(-0.1)]
    [DataRow(1.0)]
    [DataRow(2.5)]
    [DataRow(double.NaN)]
    public void Validate_WhenExpiryJitterOutOfRange_ShouldThrowArgumentException(double fraction)
    {
        var options = new CachingRateOptions { ExpiryJitter = fraction };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(CachingRateOptions.ExpiryJitter), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out string? error));
        Assert.IsNotNull(error);
    }
}
