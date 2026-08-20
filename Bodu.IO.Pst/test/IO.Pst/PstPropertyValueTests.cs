// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValueTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Text;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyValue" />: the typed accessors over a resolved wire-typed payload. This root holds
/// the value factory the member partials share.
/// </summary>
[TestClass]
public partial class PstPropertyValueTests
{
    /// <summary>
    /// Creates a value with the supplied wire type and payload, as the property context would materialize it.
    /// </summary>
    /// <param name="wireType">The wire type code.</param>
    /// <param name="data">The resolved payload.</param>
    /// <returns>The value.</returns>
    private static PstPropertyValue Value(ushort wireType, byte[] data) =>
        new(0x1001, wireType, data);

    /// <summary>
    /// Creates the little-endian bytes of a 64-bit value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns>The eight bytes.</returns>
    private static byte[] Int64Bytes(long value)
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteInt64LittleEndian(bytes, value);
        return bytes;
    }

    /// <summary>
    /// Verifies that the diagnostics form names the property, wire type, and payload length.
    /// </summary>
    [TestMethod]
    public void ToString_WhenFormatted_ShouldNamePropertyTypeAndLength()
    {
        Assert.AreEqual("0x1001 (0x001F, 14 bytes)", Value(0x001F, Encoding.Unicode.GetBytes("Sample1")).ToString());
    }
}
