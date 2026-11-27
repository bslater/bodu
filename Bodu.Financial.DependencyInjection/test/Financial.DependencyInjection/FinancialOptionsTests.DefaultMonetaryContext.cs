// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptionsTests.DefaultMonetaryContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="FinancialOptions.DefaultMonetaryContext" /> is <see langword="null" /> when the
    /// configuration does not configure it.
    /// </summary>
    [TestMethod]
    public void DefaultMonetaryContext_WhenNotConfigured_ShouldBeNull() =>
        Assert.IsNull(Bind(new()).DefaultMonetaryContext);

    /// <summary>
    /// Verifies that <see cref="FinancialOptions.DefaultMonetaryContext" /> binds its settings from configuration,
    /// reading the enumerations by member name in any case.
    /// </summary>
    [TestMethod]
    public void DefaultMonetaryContext_WhenBoundFromConfiguration_ShouldCarryTheSettings()
    {
        FinancialOptions options = Bind(new()
        {
            ["Financial:DefaultMonetaryContext:Rounding"] = "awayfromzero",
            ["Financial:DefaultMonetaryContext:ScalePolicy"] = "Custom",
            ["Financial:DefaultMonetaryContext:CustomScale"] = "3",
        });

        Assert.IsNotNull(options.DefaultMonetaryContext);
        Assert.AreEqual(MonetaryRoundingMode.AwayFromZero, options.DefaultMonetaryContext.Rounding);
        Assert.AreEqual(ScalePolicy.Custom, options.DefaultMonetaryContext.ScalePolicy);
        Assert.AreEqual(3, options.DefaultMonetaryContext.CustomScale);
    }
}
