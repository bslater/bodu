// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValueTests.GetDouble.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyValue.GetDouble" /> and <see cref="PstPropertyValue.GetSingle" />.
/// </summary>
public partial class PstPropertyValueTests
{
    /// <summary>
    /// Verifies that the 64-bit floating-point and floating-time wire types read through the double accessor.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="wireType">The wire type under test.</param>
    [TestMethod]
    [DataRow("floating64", (ushort)0x0005)]
    [DataRow("floating time", (ushort)0x0007)]
    public void GetDouble_WhenWireTypeIsEightByteFloat_ShouldReturnValue(string testName, ushort wireType)
    {
        Assert.IsNotNull(testName);

        var bytes = new byte[8];
        BinaryPrimitives.WriteDoubleLittleEndian(bytes, 2.5);

        Assert.AreEqual(2.5, Value(wireType, bytes).GetDouble());
    }

    /// <summary>
    /// Verifies that a 32-bit floating-point payload reads through the single accessor.
    /// </summary>
    [TestMethod]
    public void GetSingle_WhenWireTypeIsFloating32_ShouldReturnValue()
    {
        var bytes = new byte[4];
        BinaryPrimitives.WriteSingleLittleEndian(bytes, 1.25f);

        Assert.AreEqual(1.25f, Value(0x0004, bytes).GetSingle());
    }

    /// <summary>
    /// Verifies that reading a mismatched wire type throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetDouble_WhenWireTypeMismatches_ShouldThrowInvalidOperationException()
    {
        PstPropertyValue value = Value(0x0003, [0, 0, 0, 0]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = value.GetDouble());
    }
}
