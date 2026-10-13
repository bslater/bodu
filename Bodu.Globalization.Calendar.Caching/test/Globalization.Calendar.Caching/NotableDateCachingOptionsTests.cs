// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateCachingOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.Extensions.Logging;

namespace Bodu.Globalization.Calendar.Caching;

/// <summary>
/// Verifies the defaults and validation rules of <see cref="NotableDateCachingOptions" />, including the deterministic
/// time-to-live jitter fraction.
/// </summary>
[TestClass]
public sealed class NotableDateCachingOptionsTests
{
    /// <summary>
    /// Verifies the constructed defaults: a 30-day time-to-live, no jitter, and a valid configuration overall.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDefaults_ShouldBeValidWithExpectedValues()
    {
        var options = new NotableDateCachingOptions();

        Assert.AreEqual(TimeSpan.FromDays(30), options.Ttl);
        Assert.AreEqual(0d, options.TtlJitter);
        Assert.IsTrue(options.TryValidate(out string? error), error);
    }

    /// <summary>
    /// Verifies that jitter fractions within <c>[0, 1)</c> validate successfully.
    /// </summary>
    /// <param name="fraction">The jitter fraction to configure.</param>
    [TestMethod]
    [DataRow(0.0)]
    [DataRow(0.1)]
    [DataRow(0.999)]
    public void Validate_WhenTtlJitterInRange_ShouldNotThrow(double fraction)
    {
        var options = new NotableDateCachingOptions { TtlJitter = fraction };

        options.Validate();

        Assert.IsTrue(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that a jitter fraction outside <c>[0, 1)</c> - or not a number - is rejected with the offending
    /// parameter name, and that <see cref="NotableDateCachingOptions.TryValidate" /> agrees.
    /// </summary>
    /// <param name="fraction">The invalid jitter fraction.</param>
    [TestMethod]
    [DataRow(-0.1)]
    [DataRow(1.0)]
    [DataRow(2.5)]
    [DataRow(double.NaN)]
    public void Validate_WhenTtlJitterOutOfRange_ShouldThrowArgumentException(double fraction)
    {
        var options = new NotableDateCachingOptions { TtlJitter = fraction };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCachingOptions.TtlJitter), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out string? error));
        Assert.IsNotNull(error);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="NotableDateCachingOptions.CacheHitLogLevel" /> is rejected with that
    /// option's name, and that <see cref="NotableDateCachingOptions.TryValidate" /> agrees.
    /// </summary>
    [TestMethod]
    public void Validate_WhenCacheHitLogLevelUndefined_ShouldThrowArgumentExceptionNamingIt()
    {
        var options = new NotableDateCachingOptions { CacheHitLogLevel = (LogLevel)99 };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCachingOptions.CacheHitLogLevel), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }

    /// <summary>
    /// Verifies that an undefined <see cref="NotableDateCachingOptions.CacheMissLogLevel" /> is rejected with that
    /// option's name, not the hit level's, and that <see cref="NotableDateCachingOptions.TryValidate" /> agrees.
    /// </summary>
    [TestMethod]
    public void Validate_WhenCacheMissLogLevelUndefined_ShouldThrowArgumentExceptionNamingIt()
    {
        var options = new NotableDateCachingOptions { CacheMissLogLevel = (LogLevel)99 };

        var ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            options.Validate();
        });

        Assert.AreEqual(nameof(NotableDateCachingOptions.CacheMissLogLevel), ex.ParamName);
        Assert.IsFalse(options.TryValidate(out _));
    }
}
