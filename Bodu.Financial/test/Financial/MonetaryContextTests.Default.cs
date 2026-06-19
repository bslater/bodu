// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextTests.Default.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MonetaryContextTests
{
    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Default" /> exposes the documented default policy values.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Default_WhenInspected_ShouldExposeDocumentedDefaults()
    {
        MonetaryContext context = MonetaryContext.Default;

        Assert.AreEqual(ScalePolicy.CurrencyMinorUnits, context.ScalePolicy);
        Assert.AreEqual(CashRoundingPolicy.None, context.CashRounding);
        Assert.AreEqual(AllocationPolicy.LargestRemainder, context.Allocation);
        Assert.AreEqual(ConversionRoundingPolicy.RoundAtTarget, context.ConversionRounding);
        Assert.IsInstanceOfType<MidpointRoundingStrategy>(context.Rounding);
    }
}
