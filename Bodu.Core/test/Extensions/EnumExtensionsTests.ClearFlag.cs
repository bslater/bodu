// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.ClearFlag.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class EnumExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ClearFlag</c> returns a value with the requested bits removed.
    /// </summary>
    [TestMethod]
    public void ClearFlag_WhenFlagIsSet_ShouldRemoveTheFlag()
    {
        IntFlags value = IntFlags.A | IntFlags.B | IntFlags.C;

        Assert.AreEqual(IntFlags.A | IntFlags.C, value.ClearFlag(IntFlags.B));
    }

    /// <summary>
    /// Verifies that <c>ClearFlag</c> leaves the value unchanged when the requested bits are not set.
    /// </summary>
    [TestMethod]
    public void ClearFlag_WhenFlagIsAbsent_ShouldReturnEquivalentValue()
    {
        IntFlags value = IntFlags.A | IntFlags.B;

        Assert.AreEqual(value, value.ClearFlag(IntFlags.D));
    }

    /// <summary>
    /// Verifies that <c>ClearFlag</c> leaves other bits untouched when clearing a single flag.
    /// </summary>
    [TestMethod]
    public void ClearFlag_WhenClearingOneFlag_ShouldPreserveOtherFlags()
    {
        IntFlags value = IntFlags.A | IntFlags.B | IntFlags.D;

        Assert.AreEqual(IntFlags.A | IntFlags.D, value.ClearFlag(IntFlags.B));
    }

    /// <summary>
    /// Verifies that <c>ClearFlag</c> clears a bit beyond the 32-bit range for a <see cref="long" />-backed
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void ClearFlag_ForLongBackedEnum_ShouldClearBitAboveInt32()
    {
        LongFlags value = LongFlags.A | LongFlags.High;

        Assert.AreEqual(LongFlags.A, value.ClearFlag(LongFlags.High));
    }
}
