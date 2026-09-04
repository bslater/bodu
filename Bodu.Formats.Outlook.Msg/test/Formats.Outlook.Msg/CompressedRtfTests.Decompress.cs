// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompressedRtfTests.Decompress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

#if OUTLOOK_PST
using ExpectedFormatException = Bodu.Formats.Outlook.OutlookPstFormatException;
#else
using ExpectedFormatException = Bodu.Formats.Outlook.OutlookMsgFormatException;
#endif

#if OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#else
namespace Bodu.Formats.Outlook.Msg;
#endif

public partial class CompressedRtfTests
{
    /// <summary>
    /// Verifies that the published and reference-generated payloads decode to their expected RTF text.
    /// </summary>
    /// <param name="kat">The known-answer row.</param>
    [TestMethod]
    [DynamicData(
        nameof(DecompressKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decompress_WhenKnownPayload_ShouldReturnExpectedText(ValidKat<string, string> kat)
    {
        byte[] rtf = CompressedRtf.Decompress(Convert.FromHexString(kat.Input));

        Assert.AreEqual(kat.Expected, System.Text.Encoding.Latin1.GetString(rtf));
    }

    /// <summary>
    /// Verifies that malformed payloads throw <see cref="ExpectedFormatException" />.
    /// </summary>
    /// <param name="kat">The malformed-payload row.</param>
    [TestMethod]
    [DynamicData(
        nameof(MalformedKats),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Decompress_WhenMalformedPayload_ShouldThrowFormatException(InvalidKat<string> kat)
    {
        byte[] payload = Convert.FromHexString(kat.Input);

        _ = Assert.ThrowsExactly<ExpectedFormatException>(() =>
        {
            _ = CompressedRtf.Decompress(payload);
        });
    }

    /// <summary>
    /// Verifies that the checksum implementation matches the format's published parameters: a zero initial value with
    /// no final exclusive-or (the empty sum is zero), pinned against the specification example's recorded checksum.
    /// </summary>
    [TestMethod]
    public void ComputeCrc_WhenSpecExamplePayload_ShouldMatchRecordedChecksum()
    {
        Assert.AreEqual(0u, CompressedRtf.ComputeCrc(ReadOnlySpan<byte>.Empty));

        byte[] example = Convert.FromHexString(SpecExample1Hex);
        uint recorded = BitConverter.ToUInt32(example, 12);
        Assert.AreEqual(recorded, CompressedRtf.ComputeCrc(example.AsSpan(16)));
    }
}
