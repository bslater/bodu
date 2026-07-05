// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EvictingDictionaryExpirationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic;

[TestClass]
public class EvictingDictionaryExpirationTests
{
    /// <summary>
    /// Verifies that the single-argument constructor stores the time-to-live and defaults to absolute expiration on
    /// the system clock.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenOnlyTimeToLiveProvided_ShouldDefaultToAbsoluteKindAndSystemClock()
    {
        var expiration = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(5));

        Assert.AreEqual(TimeSpan.FromMinutes(5), expiration.TimeToLive);
        Assert.AreEqual(EvictingDictionaryExpirationKind.Absolute, expiration.Kind);
        Assert.AreSame(TimeProvider.System, expiration.TimeProvider);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> time-to-live is accepted and reported, meaning entries added without a
    /// per-entry override never expire.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTimeToLiveIsNull_ShouldStoreNullTimeToLive()
    {
        var expiration = new EvictingDictionaryExpiration(null, EvictingDictionaryExpirationKind.Sliding);

        Assert.IsNull(expiration.TimeToLive);
        Assert.AreEqual(EvictingDictionaryExpirationKind.Sliding, expiration.Kind);
    }

    /// <summary>
    /// Verifies that all constructor arguments are stored when the full overload is used.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenAllArgumentsProvided_ShouldStoreAllValues()
    {
        var expiration = new EvictingDictionaryExpiration(
            TimeSpan.FromSeconds(30),
            EvictingDictionaryExpirationKind.Sliding,
            TimeProvider.System);

        Assert.AreEqual(TimeSpan.FromSeconds(30), expiration.TimeToLive);
        Assert.AreEqual(EvictingDictionaryExpirationKind.Sliding, expiration.Kind);
        Assert.AreSame(TimeProvider.System, expiration.TimeProvider);
    }

    /// <summary>
    /// Verifies that a zero time-to-live throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTimeToLiveIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EvictingDictionaryExpiration(TimeSpan.Zero);
        });

        Assert.AreEqual("timeToLive", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a negative time-to-live throws <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTimeToLiveIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EvictingDictionaryExpiration(TimeSpan.FromSeconds(-1));
        });

        Assert.AreEqual("timeToLive", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an undefined <see cref="EvictingDictionaryExpirationKind" /> value throws
    /// <see cref="ArgumentOutOfRangeException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKindIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), (EvictingDictionaryExpirationKind)99);
        });

        Assert.AreEqual("kind", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <see langword="null" /> time provider throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenTimeProviderIsNull_ShouldThrowArgumentNullException()
    {
        var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = new EvictingDictionaryExpiration(TimeSpan.FromMinutes(1), EvictingDictionaryExpirationKind.Absolute, null);
        });

        Assert.AreEqual("timeProvider", ex.ParamName);
    }
}
