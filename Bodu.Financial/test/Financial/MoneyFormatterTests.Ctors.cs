// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyFormatterTests.Ctors.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MoneyFormatterTests
{

    /// <summary>
    /// Verifies that an undefined currency-display value is rejected.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCurrencyDisplayUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MoneyFormatter(new MoneyFormatOptions { CurrencyDisplay = (CurrencyDisplay)99 });
        });
    }

    /// <summary>
    /// Verifies that the constructor rejects a <see cref="MoneyFormatOptions.MinorUnitsOverride" /> outside 0 to 28
    /// with <see cref="ArgumentOutOfRangeException" /> naming the options, rather than deferring the failure to the
    /// <c>Format</c> methods.
    /// </summary>
    /// <param name="minorUnitsOverride">The out-of-range precision override.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(29)]
    public void Constructor_WhenMinorUnitsOverrideOutOfRange_ShouldThrowArgumentOutOfRangeException(int minorUnitsOverride)
    {
        var options = new MoneyFormatOptions { MinorUnitsOverride = minorUnitsOverride };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = new MoneyFormatter(options);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
