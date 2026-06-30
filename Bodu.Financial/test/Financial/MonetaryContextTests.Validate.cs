// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MonetaryContextTests.Validate.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

public partial class MonetaryContextTests
{

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Validate" /> rejects an undefined scale policy.
    /// </summary>
    [TestMethod]
    public void Validate_WhenScalePolicyUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = (ScalePolicy)99 };

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(context.Validate);
    }

    /// <summary>
    /// Verifies that <see cref="MonetaryContext.Validate" /> rejects a custom scale outside the supported range.
    /// </summary>
    [TestMethod]
    public void Validate_WhenCustomScaleOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = 99 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(context.Validate);
        Assert.AreEqual("CustomScale", ex.ParamName);
    }

    /// <summary>
    /// Verifies that a custom scale policy with no scale supplied is rejected by <c>Validate</c>.
    /// </summary>
    [TestMethod]
    public void Validate_WhenCustomScalePolicyWithoutScale_ShouldThrowArgumentException()
    {
        MonetaryContext context = MonetaryContext.Default with { ScalePolicy = ScalePolicy.Custom, CustomScale = null };

        Assert.ThrowsExactly<ArgumentException>(context.Validate);
    }
}
