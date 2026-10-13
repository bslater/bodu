// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyInfoTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
