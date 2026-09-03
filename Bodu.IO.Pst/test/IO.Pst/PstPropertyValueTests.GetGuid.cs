// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstPropertyValueTests.GetGuid.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst;

public partial class PstPropertyValueTests
{
    /// <summary>
    /// Verifies that a GUID payload decodes from its sixteen bytes.
    /// </summary>
    [TestMethod]
    public void GetGuid_WhenPayloadIsSixteenBytes_ShouldReturnValue()
    {
        var expected = new Guid("8b4f19a2-1f7e-4e4c-9c93-2d0305a6f0aa");

        Assert.AreEqual(expected, Value(0x0048, expected.ToByteArray()).GetGuid());
    }

    /// <summary>
    /// Verifies that a GUID payload longer than sixteen bytes — reachable through a table cell that resolves an
    /// over-long heap item — decodes from its leading sixteen bytes rather than escaping as an argument exception.
    /// </summary>
    [TestMethod]
    public void GetGuid_WhenPayloadExceedsSixteenBytes_ShouldDecodeLeadingBytes()
    {
        var expected = new Guid("8b4f19a2-1f7e-4e4c-9c93-2d0305a6f0aa");
        byte[] payload = [.. expected.ToByteArray(), 0xAA, 0xBB, 0xCC, 0xDD];

        Assert.AreEqual(expected, Value(0x0048, payload).GetGuid());
    }

    /// <summary>
    /// Verifies that a GUID payload shorter than sixteen bytes is refused through the accessor's documented
    /// <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetGuid_WhenPayloadIsShort_ShouldThrowInvalidOperationException()
    {
        PstPropertyValue value = Value(0x0048, new byte[15]);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = value.GetGuid();
        });
    }
}
