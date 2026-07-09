// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBuilderTests.Upsert.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.Upsert" /> inserts a new observation when the date is
    /// unknown.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenDateMissing_ShouldInsert()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");

        builder.Upsert(new DateOnly(2026, 6, 1), 1.50m);

        Assert.AreEqual(1, builder.Count);
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.50m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.Upsert" /> overwrites an existing observation without
    /// changing the count.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenDateExists_ShouldReplace()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        builder.Upsert(new DateOnly(2026, 6, 1), 1.75m);

        Assert.AreEqual(1, builder.Count);
        Assert.IsTrue(builder.TryGetRate(new DateOnly(2026, 6, 1), out decimal rate));
        Assert.AreEqual(1.75m, rate);
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.Upsert" /> validates the rate.
    /// </summary>
    [TestMethod]
    public void Upsert_WhenRateInvalid_ShouldThrowArgumentOutOfRangeException()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentOutOfRangeException>(
            () =>
            {
                builder.Upsert(new DateOnly(2026, 6, 1), 0m);
            },
            "rate");
    }
}
