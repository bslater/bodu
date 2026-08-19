// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValueTests.GetString.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Pst;

/// <summary>
/// Verifies <see cref="PstPropertyValue.GetString" />, <see cref="PstPropertyValue.GetGuid" />, and
/// <see cref="PstPropertyValue.GetBytes" />.
/// </summary>
public partial class PstPropertyValueTests
{
    /// <summary>
    /// Verifies that a UTF-16LE payload decodes through the string accessor, including the empty string.
    /// </summary>
    [TestMethod]
    public void GetString_WhenWireTypeIsUnicodeString_ShouldDecodeUtf16()
    {
        Assert.AreEqual("Sample1", Value(0x001F, Encoding.Unicode.GetBytes("Sample1")).GetString());
        Assert.AreEqual(string.Empty, Value(0x001F, []).GetString());
    }

    /// <summary>
    /// Verifies that the code-page string type (<c>0x001E</c>) is not decoded by the string accessor, because
    /// resolving its code page is a format-layer concern; the payload stays available as bytes.
    /// </summary>
    [TestMethod]
    public void GetString_WhenWireTypeIsCodePageString_ShouldThrowInvalidOperationException()
    {
        PstPropertyValue value = Value(0x001E, [0x41, 0x42]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() => _ = value.GetString());
        CollectionAssert.AreEqual(new byte[] { 0x41, 0x42 }, value.GetBytes());
    }

    /// <summary>
    /// Verifies that a 16-byte GUID payload reads through the GUID accessor.
    /// </summary>
    [TestMethod]
    public void GetGuid_WhenWireTypeIsGuid_ShouldReturnValue()
    {
        var guid = new Guid("8b4f19a2-1f7e-4e4c-9c93-2d0305a6f0aa");

        Assert.AreEqual(guid, Value(0x0048, guid.ToByteArray()).GetGuid());
    }

    /// <summary>
    /// Verifies that the byte accessor returns a copy of any payload regardless of wire type.
    /// </summary>
    [TestMethod]
    public void GetBytes_WhenCalled_ShouldReturnPayloadCopy()
    {
        byte[] payload = [1, 2, 3];
        PstPropertyValue value = Value(0x0102, payload);

        byte[] first = value.GetBytes();
        byte[] second = value.GetBytes();

        CollectionAssert.AreEqual(payload, first);
        Assert.AreNotSame(first, second);
    }
}
