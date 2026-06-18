// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelperTests.ThrowIfFinancialJsonPolicyUndefined.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class FinancialThrowHelperTests
{

    /// <summary>
    /// Verifies that an undefined <see cref="FinancialJsonPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfFinancialJsonPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfFinancialJsonPolicyUndefined((FinancialJsonPolicy)99));
}
