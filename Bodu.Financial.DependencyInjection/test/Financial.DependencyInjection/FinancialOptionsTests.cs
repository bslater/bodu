// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial;
using Bodu.Financial.Serialization;

namespace Bodu.Financial.DependencyInjection;

/// <summary>
/// Verifies the defaults of <see cref="FinancialOptions" />.
/// </summary>
[TestClass]
public sealed class FinancialOptionsTests
{
    /// <summary>
    /// Verifies that a freshly constructed options instance exposes the documented defaults.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenConstructed_ShouldBeStrictAndReject()
    {
        FinancialOptions options = new();

        Assert.AreEqual(FinancialJsonPolicy.Strict, options.JsonPolicy);
        Assert.AreEqual(UnknownCurrencyPolicy.Reject, options.UnknownCurrency);
    }
}
