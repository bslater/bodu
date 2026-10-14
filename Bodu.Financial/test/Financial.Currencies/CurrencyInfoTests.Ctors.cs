// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.Currencies;

public partial class CurrencyInfoTests
{
    /// <summary>
    /// Verifies that the constructor rejects a minor-unit count outside 0 to 28 with
    /// <see cref="ArgumentOutOfRangeException" /> naming <see cref="CurrencyInfo.MinorUnits" />, so no currency the
    /// library rounds for carries a scale <see cref="decimal" /> cannot represent.
    /// </summary>
    /// <param name="minorUnits">The out-of-range minor-unit count.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(int.MinValue)]
    [DataRow(29)]
    [DataRow(int.MaxValue)]
    public void Ctor_WhenMinorUnitsOutOfRange_ShouldThrowArgumentOutOfRangeException(int minorUnits)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CurrencyInfo("XQQ", minorUnits, 0m, false, null, null);
        });

        Assert.AreEqual(nameof(CurrencyInfo.MinorUnits), ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor accepts the minor-unit counts 0 and 28 at the bounds of the supported range and
    /// keeps them.
    /// </summary>
    /// <param name="minorUnits">The minor-unit count at a bound of the range.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(28)]
    public void Ctor_WhenMinorUnitsAtRangeBound_ShouldKeepMinorUnits(int minorUnits)
    {
        var info = new CurrencyInfo("XQQ", minorUnits, 0m, false, null, null);

        Assert.AreEqual(minorUnits, info.MinorUnits);
    }

    /// <summary>
    /// Verifies that the constructor rejects a cash-rounding increment that is negative, or that is not a whole number
    /// of minor units at the record's <see cref="CurrencyInfo.MinorUnits" />, with
    /// <see cref="ArgumentOutOfRangeException" /> naming <see cref="CurrencyInfo.CashRoundingIncrement" />, as the
    /// <see cref="ICurrency" /> tag path rejects the same values.
    /// </summary>
    /// <param name="minorUnits">The record's minor-unit count.</param>
    /// <param name="increment">The invalid cash-rounding increment, as invariant text.</param>
    [TestMethod]
    [DataRow(2, "-0.05")]
    [DataRow(2, "-1")]
    [DataRow(2, "0.001")]
    [DataRow(2, "0.055")]
    [DataRow(0, "0.5")]
    [DataRow(3, "0.0001")]
    public void Ctor_WhenCashRoundingIncrementInvalid_ShouldThrowArgumentOutOfRangeException(int minorUnits, string increment)
    {
        decimal value = decimal.Parse(increment, CultureInfo.InvariantCulture);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CurrencyInfo("XQQ", minorUnits, value, false, null, null);
        });

        Assert.AreEqual(nameof(CurrencyInfo.CashRoundingIncrement), ex.ParamName);
    }

    /// <summary>
    /// Verifies that the constructor accepts a cash-rounding increment of zero or of a positive whole number of minor
    /// units, including one written with trailing zeros, and keeps it.
    /// </summary>
    /// <param name="minorUnits">The record's minor-unit count.</param>
    /// <param name="increment">The valid cash-rounding increment, as invariant text.</param>
    [TestMethod]
    [DataRow(2, "0")]
    [DataRow(2, "0.05")]
    [DataRow(2, "0.050")]
    [DataRow(2, "1")]
    [DataRow(0, "5")]
    [DataRow(3, "0.005")]
    [DataRow(28, "0.0000000000000000000000000001")]
    public void Ctor_WhenCashRoundingIncrementValid_ShouldKeepIncrement(int minorUnits, string increment)
    {
        decimal value = decimal.Parse(increment, CultureInfo.InvariantCulture);

        var info = new CurrencyInfo("XQQ", minorUnits, value, false, null, null);

        Assert.AreEqual(value, info.CashRoundingIncrement);
    }

    /// <summary>
    /// Verifies that every entry in the shipped ISO 4217 catalog passes the constructor's minor-unit and
    /// cash-rounding checks when rebuilt from its own values.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenRebuildingEveryShippedEntry_ShouldReproduceTheEntry()
    {
        Assert.HasCount(184, CurrencyRegistry.All);

        foreach (CurrencyInfo shipped in CurrencyRegistry.All)
        {
            var rebuilt = new CurrencyInfo(
                shipped.IsoCode,
                shipped.MinorUnits,
                shipped.CashRoundingIncrement,
                shipped.IsHistoric,
                shipped.DemonetizedOn,
                shipped.SuccessorIsoCode,
                shipped.EnglishName,
                shipped.NumericCode);

            Assert.AreEqual(shipped, rebuilt, shipped.IsoCode);
        }
    }
}
