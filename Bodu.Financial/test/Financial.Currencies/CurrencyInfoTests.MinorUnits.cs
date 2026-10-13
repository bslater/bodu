// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoTests.MinorUnits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.Currencies;

public partial class CurrencyInfoTests
{
    /// <summary>
    /// Verifies that a <c>with</c> expression that sets <see cref="CurrencyInfo.MinorUnits" /> outside 0 to 28 throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the property, as the constructor does.
    /// </summary>
    /// <param name="minorUnits">The out-of-range minor-unit count.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(29)]
    public void MinorUnits_WhenSetOutOfRangeInWithExpression_ShouldThrowArgumentOutOfRangeException(int minorUnits)
    {
        var info = new CurrencyInfo("XQQ", 2, 0m, false, null, null);

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = info with { MinorUnits = minorUnits };
        });

        Assert.AreEqual(nameof(CurrencyInfo.MinorUnits), ex.ParamName);
    }

    /// <summary>
    /// Verifies that an object initializer that sets <see cref="CurrencyInfo.MinorUnits" /> outside 0 to 28 throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the property, as the constructor does.
    /// </summary>
    /// <param name="minorUnits">The out-of-range minor-unit count.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(29)]
    public void MinorUnits_WhenSetOutOfRangeInObjectInitializer_ShouldThrowArgumentOutOfRangeException(int minorUnits)
    {
        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new CurrencyInfo("XQQ", 2, 0m, false, null, null) { MinorUnits = minorUnits };
        });

        Assert.AreEqual(nameof(CurrencyInfo.MinorUnits), ex.ParamName);
    }

    /// <summary>
    /// Verifies that a <c>with</c> expression that sets <see cref="CurrencyInfo.MinorUnits" /> to a bound of the
    /// supported range produces a record carrying that count and leaves the source record unchanged.
    /// </summary>
    /// <param name="minorUnits">The minor-unit count at a bound of the range.</param>
    [TestMethod]
    [DataRow(0)]
    [DataRow(28)]
    public void MinorUnits_WhenSetAtRangeBoundInWithExpression_ShouldProduceThatCount(int minorUnits)
    {
        var info = new CurrencyInfo("XQQ", 2, 0m, false, null, null);

        CurrencyInfo changed = info with { MinorUnits = minorUnits };

        Assert.AreEqual(minorUnits, changed.MinorUnits);
        Assert.AreEqual(2, info.MinorUnits);
    }
}
