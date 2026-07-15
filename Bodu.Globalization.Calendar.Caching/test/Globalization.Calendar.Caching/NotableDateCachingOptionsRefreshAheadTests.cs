// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCachingOptionsRefreshAheadTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the validation contract of <see cref="NotableDateCachingOptions.RefreshAheadFraction" />.
/// </summary>
[TestClass]
public sealed class NotableDateCachingOptionsRefreshAheadTests
{
    /// <summary>
    /// Verifies that the refresh-ahead fraction defaults to zero, so refresh-ahead is disabled unless opted into.
    /// </summary>
    [TestMethod]
    public void RefreshAheadFraction_WhenDefaulted_ShouldBeZero()
    {
        var options = new NotableDateCachingOptions();

        Assert.AreEqual(0d, options.RefreshAheadFraction);
    }

    /// <summary>
    /// Verifies that refresh-ahead fractions within <c>[0, 1)</c> validate successfully.
    /// </summary>
    /// <param name="fraction">The refresh-ahead fraction to configure.</param>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(0.5)]
    [DataRow(0.999)]
    public void Validate_WhenRefreshAheadFractionInRange_ShouldNotThrow(double fraction)
    {
        var options = new NotableDateCachingOptions { RefreshAheadFraction = fraction };

        options.Validate();

        Assert.IsTrue(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that a refresh-ahead fraction outside <c>[0, 1)</c> — or not a number — is rejected with the
    /// offending parameter name, and that <see cref="NotableDateCachingOptions.TryValidate" /> agrees.
    /// </summary>
    /// <param name="fraction">The invalid refresh-ahead fraction.</param>
    [TestMethod]
    [DataRow(-0.1)]
    [DataRow(1.0)]
    [DataRow(2.5)]
    [DataRow(double.NaN)]
    public void Validate_WhenRefreshAheadFractionOutOfRange_ShouldThrowArgumentException(double fraction)
    {
        var options = new NotableDateCachingOptions { RefreshAheadFraction = fraction };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCachingOptions.RefreshAheadFraction), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out string? error));
        Assert.IsNotNull(error);
    }
}
