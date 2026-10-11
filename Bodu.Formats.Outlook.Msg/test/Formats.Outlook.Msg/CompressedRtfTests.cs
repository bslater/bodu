// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompressedRtfTests.cs" company="Bodu Pty. Ltd.">
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

/// <summary>
/// Verifies the behavior of <see cref="CompressedRtf" />, the MS-OXRTFCP decoder.
/// </summary>
[TestClass]
public partial class CompressedRtfTests
{
    /// <summary>
    /// The specification's Example 1: the compressed encoding of
    /// <c>{\rtf1\ansi\ansicpg1252\pard hello world}\r\n</c> (MS-OXRTFCP §3.1).
    /// </summary>
    internal const string SpecExample1Hex =
        "2d0000002b0000004c5a4675f1c5c7a703000a007263706731323542320af32068656c090020627705b06c647d0a800fa0";

    /// <summary>The plain text the specification's Example 1 decodes to.</summary>
    internal const string SpecExample1Text = "{\\rtf1\\ansi\\ansicpg1252\\pard hello world}\r\n";

    /// <summary>
    /// The 20 bytes of RTF text, <c>{\rtf1 Hello world.}</c>, that follow each header of
    /// <see cref="UncompressedHeaderKats" />.
    /// </summary>
    internal const string HelloWorldRtfText = "{\\rtf1 Hello world.}";

    /// <summary>The bytes of <see cref="HelloWorldRtfText" /> in hexadecimal.</summary>
    internal const string HelloWorldRtfHex = "7b5c727466312048656c6c6f20776f726c642e7d";

    /// <summary>
    /// Gets known-answer rows: compressed payloads and the RTF text they must decode to. The first row is the
    /// specification's published example; the others were produced by an independent reference implementation of the
    /// format (the compression side, which this package does not implement).
    /// </summary>
    /// <value>Rows of payload hex → expected text.</value>
    public static IEnumerable<object[]> DecompressKats =>
        new ValidKat<string, string>[]
        {
            new("SpecExample1", SpecExample1Hex, SpecExample1Text),
            new(
                "UncompressedMode",
                "370000002b0000004d454c41000000007b5c727466315c616e73695c616e7369637067313235325c706172642068656c6c6f20776f726c647d0d0a",
                SpecExample1Text),
            new(
                "ReferenceHeavyBody",
                "53000000630000004c5a467549003cc501000420546865207175a069636b206203607703a081021078206a756d700640646f7604"
                + "9020740d710b607ac07920646f672e0d5f0e624c61670b7111606e64116e2e027d1320",
                "{\\rtf1 The quick brown fox jumps over the lazy dog. The quick brown fox again and again and again.}"),
            new("UncompressedNonZeroCrc", "1c000000080000004d454c41010000007b5c727466317d0d0a", "{\\rtf1}\r\n"),
            new("UncompressedRawSizeEscapesPayload", "1c000000ff0000004d454c41000000007b5c727466317d0d0a", "{\\rtf1}\r\n"),
        }.Select(kat => new object[] { kat });

    /// <summary>
    /// Gets uncompressed (<c>MELA</c>) payloads whose header disagrees with the 20 bytes of RTF text that follow it,
    /// keyed by the field that disagrees: MS-OXRTFCP 2.2.3.1 has the reader take every byte after the header, whatever
    /// <c>RAWSIZE</c> says, and never validate the CRC.
    /// </summary>
    /// <value>Rows of payload hex → the RTF text after the header.</value>
    public static IEnumerable<object[]> UncompressedHeaderKats =>
        new ValidKat<string, string>[]
        {
            new("NonZeroCrc", "20000000140000004d454c41f1c5c7a7" + HelloWorldRtfHex, HelloWorldRtfText),
            new("RawSizeAboveData", "20000000200000004d454c4100000000" + HelloWorldRtfHex, HelloWorldRtfText),
            new("CompressedSizeBelowData", "14000000140000004d454c4100000000" + HelloWorldRtfHex, HelloWorldRtfText),
            new("RawSizeBelowData", "20000000050000004d454c4100000000" + HelloWorldRtfHex, HelloWorldRtfText),
        }.Select(kat => new object[] { kat });

    /// <summary>
    /// Gets malformed payloads that must be rejected, keyed by the failure mode.
    /// </summary>
    /// <value>Rows of payload hex expected to throw <see cref="ExpectedFormatException" />.</value>
    public static IEnumerable<object[]> MalformedKats =>
        new InvalidKat<string>[]
        {
            new("ShorterThanHeader", "2d0000002b0000004c5a4675f1c5c7", typeof(ExpectedFormatException)),
            new("UnknownMagic", "2d0000002b000000deadbeeff1c5c7a700000000", typeof(ExpectedFormatException)),
            new(
                "CompressedSizeEscapesPayload",
                "ff0000002b0000004c5a4675f1c5c7a703000a007263706731323542320af32068656c090020627705b06c647d0a800fa0",
                typeof(ExpectedFormatException)),
            new(
                "CrcMismatch",
                "2d0000002b0000004c5a4675f1c5c7a803000a007263706731323542320af32068656c090020627705b06c647d0a800fa0",
                typeof(ExpectedFormatException)),
        }.Select(kat => new object[] { kat });
}
