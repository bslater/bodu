// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FinancialThrowHelperTests.ThrowIfConversionRoundingPolicyUndefined.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Financial.Serialization;

namespace Bodu.Financial;

public partial class FinancialThrowHelperTests
{

    /// <summary>
    /// Verifies that an undefined <see cref="ConversionRoundingPolicy" /> is rejected.
    /// </summary>
    [TestMethod]
    public void ThrowIfConversionRoundingPolicyUndefined_WhenUndefined_ShouldThrow() =>
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => FinancialThrowHelper.ThrowIfConversionRoundingPolicyUndefined((ConversionRoundingPolicy)99));
}
