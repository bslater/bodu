// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextOptionsTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.DependencyInjection;

public sealed partial class MonetaryContextOptionsTests
{
    /// <summary>
    /// Verifies that a new <see cref="MonetaryContextOptions" /> has the default of each
    /// <see cref="MonetaryContext" /> member it configures.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCreated_ShouldHaveTheMonetaryContextDefaults()
    {
        var options = new MonetaryContextOptions();

        Assert.AreEqual(MonetaryRoundingMode.ToEven, options.Rounding);
        Assert.AreEqual(MonetaryContext.Default.ScalePolicy, options.ScalePolicy);
        Assert.AreEqual(MonetaryContext.Default.CustomScale, options.CustomScale);
        Assert.AreEqual(MonetaryContext.Default.CashRounding, options.CashRounding);
        Assert.AreEqual(MonetaryContext.Default.Allocation, options.Allocation);
        Assert.AreEqual(MonetaryContext.Default.ConversionRounding, options.ConversionRounding);
    }
}
