// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.SetFlag.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class EnumExtensionsTests
{
    /// <summary>
    /// Verifies that <c>SetFlag</c> returns a value with the requested bits added to the original.
    /// </summary>
    [TestMethod]
    public void SetFlag_WhenFlagIsAbsent_ShouldAddTheFlag()
    {
        IntFlags result = IntFlags.A.SetFlag(IntFlags.C);

        Assert.AreEqual(IntFlags.A | IntFlags.C, result);
    }

    /// <summary>
    /// Verifies that <c>SetFlag</c> leaves the value unchanged when the requested bits are already set.
    /// </summary>
    [TestMethod]
    public void SetFlag_WhenFlagIsAlreadySet_ShouldReturnEquivalentValue()
    {
        IntFlags value = IntFlags.A | IntFlags.B;

        Assert.AreEqual(value, value.SetFlag(IntFlags.B));
    }

    /// <summary>
    /// Verifies that <c>SetFlag</c> sets a bit beyond the 32-bit range for a <see cref="long" />-backed
    /// enumeration.
    /// </summary>
    [TestMethod]
    public void SetFlag_ForLongBackedEnum_ShouldSetBitAboveInt32()
    {
        LongFlags result = LongFlags.A.SetFlag(LongFlags.High);

        Assert.AreEqual(LongFlags.A | LongFlags.High, result);
    }
}
