// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValueTests.GetInt64.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyValue.GetInt64" />.
/// </summary>
public partial class PstPropertyValueTests
{
    /// <summary>
    /// Verifies that the 64-bit integer, currency, and FILETIME wire types all read through the 64-bit accessor.
    /// </summary>
    /// <param name="testName">The scenario name.</param>
    /// <param name="wireType">The wire type under test.</param>
    [TestMethod]
    [DataRow("integer64", (ushort)0x0014)]
    [DataRow("currency", (ushort)0x0006)]
    [DataRow("filetime", (ushort)0x0040)]
    public void GetInt64_WhenWireTypeIsEightByte_ShouldReturnValue(string testName, ushort wireType)
    {
        Assert.IsNotNull(testName);

        Assert.AreEqual(0x1122334455667788, Value(wireType, Int64Bytes(0x1122334455667788)).GetInt64());
    }

    /// <summary>
    /// Verifies that a payload shorter than eight bytes is rejected rather than read past its end.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenPayloadIsTooShort_ShouldThrowInvalidOperationException()
    {
        PstPropertyValue value = Value(0x0014, [1, 2, 3, 4]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = value.GetInt64());
    }
}
