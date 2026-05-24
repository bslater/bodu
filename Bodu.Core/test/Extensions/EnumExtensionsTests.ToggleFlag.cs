// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.ToggleFlag.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class EnumExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ToggleFlag</c> sets a bit that was previously clear.
    /// </summary>
    [TestMethod]
    public void ToggleFlag_WhenFlagIsClear_ShouldSetTheFlag()
    {
        IntFlags result = IntFlags.A.ToggleFlag(IntFlags.C);

        Assert.AreEqual(IntFlags.A | IntFlags.C, result);
    }

    /// <summary>
    /// Verifies that <c>ToggleFlag</c> clears a bit that was previously set.
    /// </summary>
    [TestMethod]
    public void ToggleFlag_WhenFlagIsSet_ShouldClearTheFlag()
    {
        IntFlags value = IntFlags.A | IntFlags.C;

        Assert.AreEqual(IntFlags.A, value.ToggleFlag(IntFlags.C));
    }

    /// <summary>
    /// Verifies that toggling the same flags twice restores the original value.
    /// </summary>
    [TestMethod]
    public void ToggleFlag_WhenAppliedTwice_ShouldRestoreOriginalValue()
    {
        IntFlags value = IntFlags.A | IntFlags.B;

        Assert.AreEqual(value, value.ToggleFlag(IntFlags.C | IntFlags.D).ToggleFlag(IntFlags.C | IntFlags.D));
    }

    /// <summary>
    /// Verifies that <c>ToggleFlag</c> flips a bit beyond the 32-bit range for a <see cref="long" />-backed
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void ToggleFlag_ForLongBackedEnum_ShouldFlipBitAboveInt32()
    {
        Assert.AreEqual(LongFlags.A | LongFlags.High, LongFlags.A.ToggleFlag(LongFlags.High));
        Assert.AreEqual(LongFlags.A, (LongFlags.A | LongFlags.High).ToggleFlag(LongFlags.High));
    }
}
