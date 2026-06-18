// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelperTests.ThrowIfCashRoundingPolicyUndefined.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class FinancialThrowHelperTests
{
    /// <summary>
    /// Verifies that an undefined <see cref="CashRoundingPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfCashRoundingPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfCashRoundingPolicyUndefined((CashRoundingPolicy)99));
}
