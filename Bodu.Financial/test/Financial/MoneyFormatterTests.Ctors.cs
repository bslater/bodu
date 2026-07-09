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
}
