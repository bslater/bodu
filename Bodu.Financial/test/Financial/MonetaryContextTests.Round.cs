// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextTests.Round.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MonetaryContextTests
{

    /// <summary>
    /// Verifies that the rounding strategy honors the configured midpoint mode.
    /// </summary>
    [TestMethod]
    public void Round_WhenAwayFromZeroStrategy_ShouldRoundMidpointAway()
    {
        MonetaryContext context = MonetaryContext.Default with { Rounding = MidpointRoundingStrategy.AwayFromZero };

        Assert.AreEqual(1.23m, context.Round(1.225m, 2));
        Assert.AreEqual(1.22m, MonetaryContext.Default.Round(1.225m, 2));
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Round(decimal, int)" /> rejects a custom scale outside 0 to 28 with
    /// <see cref="ArgumentOutOfRangeException" /> naming <see cref="MonetaryContext.CustomScale" />, as
    /// <see cref="MonetaryContext.Validate" /> does, rather than returning the value unrounded or failing in the
    /// rounding strategy.
    /// </summary>
    /// <param name="customScale">The out-of-range custom scale.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(-2)]
    [DataRow(29)]
    public void Round_WhenCustomScaleOutOfRange_ShouldThrowArgumentOutOfRangeException(int customScale)
    {
        MonetaryContext context =
            MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = customScale };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = context.Round(1.234m, 2);
        });

        Assert.AreEqual(nameof(MonetaryContext.CustomScale), ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Round(decimal, int)" /> rejects a currency minor-unit count outside 0 to
    /// 28 with <see cref="ArgumentOutOfRangeException" /> naming the argument, whatever the scale policy, rather than
    /// returning the value unrounded or failing in the rounding strategy.
    /// </summary>
    /// <param name="policy">The scale policy in effect.</param>
    /// <param name="currencyMinorUnits">The out-of-range minor-unit count.</param>
    [TestMethod]
    [DataRow(ScalePolicy.CurrencyMinorUnits, -1)]
    [DataRow(ScalePolicy.CurrencyMinorUnits, int.MinValue)]
    [DataRow(ScalePolicy.CurrencyMinorUnits, 29)]
    [DataRow(ScalePolicy.Unrounded, -1)]
    [DataRow(ScalePolicy.Unrounded, int.MinValue)]
    [DataRow(ScalePolicy.Unrounded, 29)]
    [DataRow(ScalePolicy.Custom, -1)]
    [DataRow(ScalePolicy.Custom, int.MinValue)]
    [DataRow(ScalePolicy.Custom, 29)]
    public void Round_WhenCurrencyMinorUnitsOutOfRange_ShouldThrowArgumentOutOfRangeException(
        ScalePolicy policy,
        int currencyMinorUnits)
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = policy, CustomScale = 2 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = context.Round(1.234m, currencyMinorUnits);
        });

        Assert.AreEqual(nameof(currencyMinorUnits), ex.ParamName);
    }
}
