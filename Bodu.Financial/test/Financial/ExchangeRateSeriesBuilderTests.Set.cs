// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateSeriesBuilderTests.Set.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial;

public partial class ExchangeRateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.Set" /> overwrites the rate for an existing date.
    /// </summary>
    [TestMethod]
    public void Set_WhenDateExists_ShouldOverwriteRate()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        builder.Set(new DateOnly(2026, 6, 1), 1.75m);

        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out var rate));
        Assert.AreEqual(1.75m, rate);
        Assert.AreEqual(1, builder.Count);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.Set" /> on an unknown date throws
    /// <see cref="KeyNotFoundException" />.
    /// </summary>
    [TestMethod]
    public void Set_WhenDateMissing_ShouldThrowKeyNotFoundException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            builder.Set(new DateOnly(2026, 6, 1), 1.50m);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.Set" /> validates the rate.
    /// </summary>
    [TestMethod]
    public void Set_WhenRateIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.Set(new DateOnly(2026, 6, 1), 0m);
            },
            "rate");
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TrySet" /> overwrites and returns <see langword="true" />
    /// when the date exists.
    /// </summary>
    [TestMethod]
    public void TrySet_WhenDateExists_ShouldOverwriteAndReturnTrue()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        var updated = builder.TrySet(new DateOnly(2026, 6, 1), 1.75m);

        Assert.IsTrue(updated);
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out var rate));
        Assert.AreEqual(1.75m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TrySet" /> returns <see langword="false" /> when the date
    /// is unknown and does not mutate the builder.
    /// </summary>
    [TestMethod]
    public void TrySet_WhenDateMissing_ShouldReturnFalse()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");

        var updated = builder.TrySet(new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsFalse(updated);
        Assert.IsTrue(builder.IsEmpty);
    }

    /// <summary>
    /// Verifies that <see cref="ExchangeRateSeriesBuilder.TrySet" /> validates the rate.
    /// </summary>
    [TestMethod]
    public void TrySet_WhenRateInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        ExchangeRateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.TrySet(new DateOnly(2026, 6, 1), -0.01m);
            },
            "rate");
    }
}
