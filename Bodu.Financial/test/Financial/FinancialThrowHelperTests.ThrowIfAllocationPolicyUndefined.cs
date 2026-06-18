// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelperTests.ThrowIfAllocationPolicyUndefined.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class FinancialThrowHelperTests
{

    /// <summary>
    /// Verifies that an undefined <see cref="AllocationPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfAllocationPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfAllocationPolicyUndefined((AllocationPolicy)99));
}
