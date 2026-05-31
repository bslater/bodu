// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.HasAllFlags.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class EnumExtensionsTests
{
    /// <summary>
    /// Verifies that <c>HasAllFlags</c> returns <see langword="true" /> when every bit in the flags argument is set
    /// in the value.
    /// </summary>
    [TestMethod]
    public void HasAllFlags_WhenAllFlagsPresent_ShouldReturnTrue()
    {
        IntFlags value = IntFlags.A | IntFlags.B | IntFlags.C;

        Assert.IsTrue(value.HasAllFlags(IntFlags.A | IntFlags.C));
    }

    /// <summary>
    /// Verifies that <c>HasAllFlags</c> returns <see langword="false" /> when only some of the requested bits are
    /// set in the value.
    /// </summary>
    [TestMethod]
    public void HasAllFlags_WhenSomeFlagsMissing_ShouldReturnFalse()
    {
        IntFlags value = IntFlags.A | IntFlags.B;

        Assert.IsFalse(value.HasAllFlags(IntFlags.A | IntFlags.D));
    }

    /// <summary>
    /// Verifies that <c>HasAllFlags</c> returns <see langword="true" /> when the flags argument has no bits set.
    /// </summary>
    [TestMethod]
    public void HasAllFlags_WhenFlagsIsNone_ShouldReturnTrue()
    {
        Assert.IsTrue(IntFlags.A.HasAllFlags(IntFlags.None));
    }

    /// <summary>
    /// Verifies that <c>HasAllFlags</c> evaluates a bit beyond the 32-bit range for a <see cref="long" />-backed
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void HasAllFlags_ForLongBackedEnum_ShouldEvaluateBitAboveInt32()
    {
        LongFlags value = LongFlags.High | LongFlags.A;

        Assert.IsTrue(value.HasAllFlags(LongFlags.High | LongFlags.A));
        Assert.IsFalse(value.HasAllFlags(LongFlags.High | LongFlags.B));
    }
}
