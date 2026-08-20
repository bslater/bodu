// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValueTests.GetInt32.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyValue.GetInt32" /> and <see cref="PstPropertyValue.GetInt16" />.
/// </summary>
public partial class PstPropertyValueTests
{
    /// <summary>
    /// Verifies that a 32-bit integer payload reads back its little-endian value.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenWireTypeIsInteger32_ShouldReturnValue()
    {
        Assert.AreEqual(-559038737, Value(0x0003, [0xEF, 0xBE, 0xAD, 0xDE]).GetInt32());
    }

    /// <summary>
    /// Verifies that the 32-bit error-code wire type also reads through the 32-bit accessor.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenWireTypeIsErrorCode_ShouldReturnValue()
    {
        Assert.AreEqual(unchecked((int)0x8004010F), Value(0x000A, [0x0F, 0x01, 0x04, 0x80]).GetInt32());
    }

    /// <summary>
    /// Verifies that a 16-bit integer payload reads back its little-endian value.
    /// </summary>
    [TestMethod]
    public void GetInt16_WhenWireTypeIsInteger16_ShouldReturnValue()
    {
        Assert.AreEqual((short)0x1234, Value(0x0002, [0x34, 0x12]).GetInt16());
    }

    /// <summary>
    /// Verifies that reading a mismatched wire type through the 32-bit accessor throws
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt32_WhenWireTypeMismatches_ShouldThrowInvalidOperationException()
    {
        PstPropertyValue value = Value(0x001F, [0x41, 0x00]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = value.GetInt32());
    }
}
