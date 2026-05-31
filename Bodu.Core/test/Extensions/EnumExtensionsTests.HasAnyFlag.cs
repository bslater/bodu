// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.HasAnyFlag.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class EnumExtensionsTests
{
    /// <summary>
    /// Verifies that <c>HasAnyFlag</c> returns <see langword="true" /> when the value and the flags share at least
    /// one set bit.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void HasAnyFlag_WhenValueSharesABit_ShouldReturnTrue()
    {
        IntFlags value = IntFlags.A | IntFlags.B;

        Assert.IsTrue(value.HasAnyFlag(IntFlags.B | IntFlags.C));
    }

    /// <summary>
    /// Verifies that <c>HasAnyFlag</c> returns <see langword="false" /> when the value and the flags share no bits.
    /// </summary>
    [TestMethod]
    public void HasAnyFlag_WhenValueSharesNoBit_ShouldReturnFalse()
    {
        Assert.IsFalse(IntFlags.A.HasAnyFlag(IntFlags.B | IntFlags.C));
    }

    /// <summary>
    /// Verifies that <c>HasAnyFlag</c> returns <see langword="false" /> when the flags argument has no bits set.
    /// </summary>
    [TestMethod]
    public void HasAnyFlag_WhenFlagsIsNone_ShouldReturnFalse()
    {
        IntFlags value = IntFlags.A | IntFlags.B;

        Assert.IsFalse(value.HasAnyFlag(IntFlags.None));
    }

    /// <summary>
    /// Verifies that <c>HasAnyFlag</c> evaluates the most significant bit of a <see cref="byte" />-backed
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void HasAnyFlag_ForByteBackedEnum_ShouldEvaluateHighBit()
    {
        Assert.IsTrue((ByteFlags.High | ByteFlags.A).HasAnyFlag(ByteFlags.High));
        Assert.IsFalse((ByteFlags.A | ByteFlags.B).HasAnyFlag(ByteFlags.High));
    }

    /// <summary>
    /// Verifies that <c>HasAnyFlag</c> evaluates a bit beyond the 32-bit range for a <see cref="long" />-backed
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void HasAnyFlag_ForLongBackedEnum_ShouldEvaluateBitAboveInt32()
    {
        Assert.IsTrue((LongFlags.High | LongFlags.A).HasAnyFlag(LongFlags.High));
        Assert.IsFalse((LongFlags.A | LongFlags.B).HasAnyFlag(LongFlags.High));
    }
}
