// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EnumExtensionsTests.BitConversion.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class EnumExtensionsTests
{
    /// <summary>A flag enumeration backed by <see cref="ushort" />, exercising the two-byte reinterpretation path.</summary>
    [Flags]
    internal enum UShortFlags : ushort
    {
        /// <summary>No flag bits set.</summary>
        None = 0,

        /// <summary>The first flag bit.</summary>
        A = 1,

        /// <summary>The second flag bit.</summary>
        B = 1 << 1,

        /// <summary>The most significant bit of the underlying <see cref="ushort" />.</summary>
        High = 1 << 15,
    }

    /// <summary>
    /// Verifies that a bitwise set on a <see cref="byte" />-backed enum round-trips through the one-byte reinterpretation
    /// path.
    /// </summary>
    [TestMethod]
    public void SetFlag_OnByteBackedEnum_ShouldReinterpretByteWidth()
    {
        Assert.AreEqual(ByteFlags.A | ByteFlags.High, ByteFlags.A.SetFlag(ByteFlags.High));
    }

    /// <summary>
    /// Verifies that a bitwise set on a <see cref="ushort" />-backed enum round-trips through the two-byte
    /// reinterpretation path.
    /// </summary>
    [TestMethod]
    public void SetFlag_OnUShortBackedEnum_ShouldReinterpretTwoByteWidth()
    {
        Assert.AreEqual(UShortFlags.A | UShortFlags.High, UShortFlags.A.SetFlag(UShortFlags.High));
    }

    /// <summary>
    /// Verifies that a flag test on a <see cref="ushort" />-backed enum reads the two-byte reinterpretation path.
    /// </summary>
    [TestMethod]
    public void HasAnyFlag_OnUShortBackedEnum_ShouldReinterpretTwoByteWidth()
    {
        Assert.IsTrue((UShortFlags.A | UShortFlags.High).HasAnyFlag(UShortFlags.High));
    }
}
