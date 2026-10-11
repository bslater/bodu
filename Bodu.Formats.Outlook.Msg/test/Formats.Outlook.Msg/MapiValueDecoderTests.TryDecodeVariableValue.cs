// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiValueDecoderTests.TryDecodeVariableValue.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

#if OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#else
namespace Bodu.Formats.Outlook.Msg;
#endif

public partial class MapiValueDecoderTests
{
    /// <summary>
    /// Verifies that a null payload is reported as undecodable rather than escaping as a null-reference failure -
    /// the decoder's documented contract is that it never throws.
    /// </summary>
    [TestMethod]
    public void TryDecodeVariableValue_WhenBytesNull_ShouldReturnFalse()
    {
        Assert.IsFalse(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.Unicode, null!, Encoding.Unicode, strict: false, out object? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that a null encoding is reported as undecodable for a code-page string rather than escaping as a
    /// null-reference failure.
    /// </summary>
    [TestMethod]
    public void TryDecodeVariableValue_WhenEncodingNull_ShouldReturnFalse()
    {
        Assert.IsFalse(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.String8, [0x41], null!, strict: false, out object? value));
        Assert.IsNull(value);
    }

    /// <summary>
    /// Verifies that a UTF-8 code-page string carrying a leading byte-order mark decodes without the mark - the
    /// encoding's <c>GetString</c> does not strip it, so the decoder must.
    /// </summary>
    [TestMethod]
    public void TryDecodeVariableValue_WhenUtf8PayloadCarriesByteOrderMark_ShouldStripIt()
    {
        byte[] payload = [0xEF, 0xBB, 0xBF, (byte)'a', (byte)'b'];
        Encoding utf8 = MapiEncodingResolver.GetEncoding(65001, null);

        Assert.IsTrue(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.String8, payload, utf8, strict: false, out object? value));
        Assert.AreEqual("ab", value);
    }

    /// <summary>
    /// Verifies that a <c>Unicode</c> or <c>String8</c> payload ends at its first NUL terminator, as MS-OXCDATA 2.11.1
    /// defines both types: the terminator and the bytes after it are not part of the text, at either strictness.
    /// </summary>
    /// <param name="testName">The strictness, by name.</param>
    /// <param name="strict">Whether the payload is decoded strictly.</param>
    [TestMethod]
    [DataRow("Tolerant", false)]
    [DataRow("Strict", true)]
    public void TryDecodeVariableValue_WhenStringHoldsBytesAfterItsTerminator_ShouldEndAtTheTerminator(string testName, bool strict)
    {
        byte[] unicode = Encoding.Unicode.GetBytes("Hello\0junk");
        byte[] ansi = Encoding.ASCII.GetBytes("Hello\0junk");
        Encoding windows1252 = MapiEncodingResolver.GetEncoding(1252, null);

        Assert.IsTrue(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.Unicode, unicode, Encoding.Unicode, strict, out object? text), testName);
        Assert.AreEqual("Hello", text, testName);
        Assert.IsTrue(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.String8, ansi, windows1252, strict, out object? ansiText), testName);
        Assert.AreEqual("Hello", ansiText, testName);
    }

    /// <summary>
    /// Verifies that a <c>Unicode</c> payload is cut only at a NUL code unit: zero bytes inside nonzero code units, such
    /// as those of U+0100 and U+4100, are text.
    /// </summary>
    [TestMethod]
    public void TryDecodeVariableValue_WhenUnicodeCodeUnitsHoldZeroBytes_ShouldKeepThem()
    {
        byte[] payload = Encoding.Unicode.GetBytes("\u0100\u4100x");

        Assert.IsTrue(MapiValueDecoder.TryDecodeVariableValue(MapiPropertyType.Unicode, payload, Encoding.Unicode, strict: true, out object? value));
        Assert.AreEqual("\u0100\u4100x", value);
    }
}
