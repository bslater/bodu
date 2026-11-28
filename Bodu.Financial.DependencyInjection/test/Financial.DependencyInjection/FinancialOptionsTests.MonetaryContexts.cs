// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptionsTests.MonetaryContexts.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.DependencyInjection;

public sealed partial class FinancialOptionsTests
{
    /// <summary>
    /// Verifies that <see cref="FinancialOptions.MonetaryContexts" /> is empty on a new instance.
    /// </summary>
    [TestMethod]
    public void MonetaryContexts_WhenCreated_ShouldBeEmpty() =>
        Assert.IsEmpty(new FinancialOptions().MonetaryContexts);

    /// <summary>
    /// Verifies that <see cref="FinancialOptions.MonetaryContexts" /> binds one entry for each named section under the
    /// configuration's <c>MonetaryContexts</c> section.
    /// </summary>
    [TestMethod]
    public void MonetaryContexts_WhenBoundFromConfiguration_ShouldHoldEachNamedContext()
    {
        FinancialOptions options = Bind(new()
        {
            ["Financial:MonetaryContexts:Invoicing:Rounding"] = "ToZero",
            ["Financial:MonetaryContexts:CashDesk:CashRounding"] = "CurrencyCashIncrement",
        });

        Assert.HasCount(2, options.MonetaryContexts);
        Assert.AreEqual(MonetaryRoundingMode.ToZero, options.MonetaryContexts["Invoicing"].Rounding);
        Assert.AreEqual(CashRoundingPolicy.CurrencyCashIncrement, options.MonetaryContexts["CashDesk"].CashRounding);
    }

    /// <summary>
    /// Verifies that <see cref="FinancialOptions.MonetaryContexts" /> compares names case-insensitively, as
    /// configuration keys compare.
    /// </summary>
    [TestMethod]
    public void MonetaryContexts_WhenLookedUpInAnotherCase_ShouldFindTheContext()
    {
        FinancialOptions options = Bind(new() { ["Financial:MonetaryContexts:Invoicing:Rounding"] = "ToZero" });

        Assert.IsTrue(options.MonetaryContexts.ContainsKey("INVOICING"));
    }
}
