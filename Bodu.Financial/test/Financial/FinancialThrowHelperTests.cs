// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelperTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Serialization;

namespace Bodu.Financial;

/// <summary>
/// Verifies the policy-validation guards on <see cref="FinancialThrowHelper" /> reject undefined enum values with
/// <see cref="ArgumentOutOfRangeException" />.
/// </summary>
[TestClass]
public class FinancialThrowHelperTests
{
    /// <summary>
    /// Verifies that an undefined <see cref="CashRoundingPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfCashRoundingPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfCashRoundingPolicyUndefined((CashRoundingPolicy)99));

    /// <summary>
    /// Verifies that an undefined <see cref="AllocationPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfAllocationPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfAllocationPolicyUndefined((AllocationPolicy)99));

    /// <summary>
    /// Verifies that an undefined <see cref="ConversionRoundingPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfConversionRoundingPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfConversionRoundingPolicyUndefined((ConversionRoundingPolicy)99));

    /// <summary>
    /// Verifies that an undefined <see cref="MoneyBagConversionRoundingPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfMoneyBagRoundingPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfMoneyBagRoundingPolicyUndefined((MoneyBagConversionRoundingPolicy)99));

    /// <summary>
    /// Verifies that an undefined <see cref="FinancialJsonPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfFinancialJsonPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfFinancialJsonPolicyUndefined((FinancialJsonPolicy)99));
}
