// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompressedRtfTests.Limits.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.Formats.Outlook.Msg;

public partial class CompressedRtfTests
{
    /// <summary>
    /// Verifies that a header declaring a multi-gigabyte uncompressed size for a few bytes of body is rejected as
    /// malformed before any output buffer is sized from it: the declared size is not covered by the checksum, so it
    /// must be bounded by what the body can physically expand to.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Regression)]
    public void Decompress_WhenDeclaredRawSizeIsHuge_ShouldThrowWithoutAllocating()
    {
        const long CeilingBytes = 16L * 1024 * 1024;
        byte[] payload = BuildPayload(BuildLiteralBody(32), int.MaxValue - 15);

        GC.Collect();
        GC.WaitForPendingFinalizers();
        long baseline = GC.GetTotalMemory(forceFullCollection: true);

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = CompressedRtf.Decompress(payload);
        });

        long peak = GC.GetTotalMemory(forceFullCollection: false) - baseline;
        Assert.IsTrue(peak < CeilingBytes, $"Rejecting the header allocated {peak / (1024 * 1024)} MB.");
    }

    /// <summary>
    /// Verifies that a token stream producing more bytes than the declared uncompressed size stops at the declared
    /// size — the header's size is the output's size, not a hint.
    /// </summary>
    [TestMethod]
    public void Decompress_WhenTokenStreamExpandsPastDeclaredSize_ShouldStopAtDeclaredSize()
    {
        byte[] payload = BuildPayload(BuildLiteralBody(100), 40);

        byte[] decoded = CompressedRtf.Decompress(payload);

        Assert.AreEqual(40, decoded.Length);
    }

    /// <summary>
    /// Verifies that a reference token cut short by the end of the body is a format error rather than a silent
    /// partial result.
    /// </summary>
    [TestMethod]
    public void Decompress_WhenReferenceTokenIsTruncated_ShouldThrowOutlookMsgFormatException()
    {
        byte[] payload = BuildPayload([0x01, 0x12], 16);

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = CompressedRtf.Decompress(payload);
        });
    }

    /// <summary>
    /// Verifies that a control byte promising a literal the body no longer holds is a format error rather than a
    /// silent partial result.
    /// </summary>
    [TestMethod]
    public void Decompress_WhenLiteralTokenIsTruncated_ShouldThrowOutlookMsgFormatException()
    {
        byte[] payload = BuildPayload([0x00, (byte)'a'], 16);

        _ = Assert.ThrowsExactly<OutlookMsgFormatException>(() =>
        {
            _ = CompressedRtf.Decompress(payload);
        });
    }
}
