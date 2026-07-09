// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RateSeriesBuilderTests.Remove.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates;

public partial class RateSeriesBuilderTests
{
    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.Remove" /> removes an existing observation and reports
    /// success.
    /// </summary>
    [TestMethod]
    public void Remove_WhenDateExists_ShouldRemoveAndReturnTrue()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);
        builder.Add(new DateOnly(2026, 6, 3), 1.52m);

        bool removed = builder.Remove(new DateOnly(2026, 6, 1));

        Assert.IsTrue(removed);
        Assert.AreEqual(1, builder.Count);
        Assert.IsFalse(builder.ContainsDate(new DateOnly(2026, 6, 1)));
        Assert.IsTrue(builder.ContainsDate(new DateOnly(2026, 6, 3)));
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.Remove" /> reports failure when the date is unknown.
    /// </summary>
    [TestMethod]
    public void Remove_WhenDateMissing_ShouldReturnFalse()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        bool removed = builder.Remove(new DateOnly(2026, 6, 2));

        Assert.IsFalse(removed);
        Assert.AreEqual(1, builder.Count);
    }

    /// <summary>
    /// Verifies that repeatedly removing the first observation maintains strictly ascending order in the remaining
    /// buffer.
    /// </summary>
    [TestMethod]
    public void Remove_WhenRepeatedlyRemovingFromBeginning_ShouldMaintainOrder()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.AddRange(SampleObservations());

        builder.Remove(new DateOnly(2026, 6, 1));
        builder.Remove(new DateOnly(2026, 6, 3));

        RateObservation[] expected = [new(new DateOnly(2026, 6, 5), 1.54m)];

        CollectionAssert.AreEqual(expected, builder.GetObservations().ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.ContainsDate" /> reports <see langword="true" /> for a
    /// present observation.
    /// </summary>
    [TestMethod]
    public void ContainsDate_WhenDateExists_ShouldReturnTrue()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");
        builder.Add(new DateOnly(2026, 6, 1), 1.50m);

        Assert.IsTrue(builder.ContainsDate(new DateOnly(2026, 6, 1)));
    }

    /// <summary>
    /// Verifies that <see cref="RateSeriesBuilder.ContainsDate" /> reports <see langword="false" /> for a
    /// missing observation.
    /// </summary>
    [TestMethod]
    public void ContainsDate_WhenDateMissing_ShouldReturnFalse()
    {
        RateSeriesBuilder builder = new(s_usdAud, "RBA");

        Assert.IsFalse(builder.ContainsDate(new DateOnly(2026, 6, 1)));
    }
}
