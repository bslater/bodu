// ---------------------------------------------------------------------------------------------------------------
// <copyright file="QuotedPrintableTests.Encode.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Encoding;

public sealed partial class QuotedPrintableTests
{
    /// <summary>
    /// Verifies that the equals sign is always escaped as <c>=3D</c>.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEqualsSign_ShouldEscapeAs3D()
    {
        Assert.AreEqual("=3D", QuotedPrintable.Encode(new byte[] { 0x3D }));
    }

    /// <summary>
    /// Verifies that a form feed (<c>0x0C</c>) is escaped as <c>=0C</c> with uppercase hex.
    /// </summary>
    [TestMethod]
    public void Encode_WhenFormFeed_ShouldEscapeAs0C()
    {
        Assert.AreEqual("=0C", QuotedPrintable.Encode(new byte[] { 0x0C }));
    }

    /// <summary>
    /// Verifies that printable ASCII (excluding the equals sign) passes through literally.
    /// </summary>
    [TestMethod]
    public void Encode_WhenPrintableAscii_ShouldPassThroughLiterally()
    {
        Assert.AreEqual("Hello, World!", QuotedPrintable.Encode(Ascii("Hello, World!")));
    }

    /// <summary>
    /// Verifies that a non-ASCII byte is escaped using uppercase hexadecimal digits.
    /// </summary>
    [TestMethod]
    public void Encode_WhenNonAsciiByte_ShouldEscapeWithUppercaseHex()
    {
        Assert.AreEqual("=E9", QuotedPrintable.Encode(new byte[] { 0xE9 }));
        Assert.AreEqual("=FF", QuotedPrintable.Encode(new byte[] { 0xFF }));
    }

    /// <summary>
    /// Verifies that space and tab in the middle of a line are emitted literally.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSpaceOrTabMidLine_ShouldEmitLiterally()
    {
        Assert.AreEqual("a b", QuotedPrintable.Encode(Ascii("a b")));
        Assert.AreEqual("a\tb", QuotedPrintable.Encode(Ascii("a\tb")));
    }

    /// <summary>
    /// Verifies that a space or tab at the very end of the input is escaped, since a decoder may delete trailing
    /// whitespace.
    /// </summary>
    [TestMethod]
    public void Encode_WhenSpaceOrTabAtEnd_ShouldEscape()
    {
        Assert.AreEqual("a=20", QuotedPrintable.Encode(Ascii("a ")));
        Assert.AreEqual("a=09", QuotedPrintable.Encode(Ascii("a\t")));
    }

    /// <summary>
    /// Verifies that encoded lines never exceed 76 characters, with the trailing soft-break marker counted within
    /// the limit.
    /// </summary>
    [TestMethod]
    public void Encode_WhenLongInput_ShouldWrapWithinSeventySixColumns()
    {
        string encoded = QuotedPrintable.Encode(Ascii(new string('A', 200)));

        string[] lines = encoded.Split("\r\n");
        foreach (string line in lines)
            Assert.IsTrue(line.Length <= 76, $"Line exceeded 76 characters ({line.Length}).");

        // Soft-wrapped lines end with '=' and reach the 76-column limit.
        Assert.IsTrue(lines[0].EndsWith('='), "Wrapped line should end with the soft-break marker.");
        Assert.AreEqual(76, lines[0].Length);
    }

    /// <summary>
    /// Verifies that text mode preserves a canonical CRLF hard break, emitting the configured newline rather than
    /// escaping it.
    /// </summary>
    [TestMethod]
    public void Encode_WhenTextModeAndCrlf_ShouldPreserveHardBreak()
    {
        string encoded = QuotedPrintable.Encode(Ascii("line1\r\nline2"), TextOptions());

        Assert.AreEqual("line1\r\nline2", encoded);
    }

    /// <summary>
    /// Verifies that text mode escapes a lone CR or lone LF rather than treating it as a hard break.
    /// </summary>
    [TestMethod]
    public void Encode_WhenTextModeAndLoneCrOrLf_ShouldEscape()
    {
        Assert.AreEqual("a=0Db", QuotedPrintable.Encode(Ascii("a\rb"), TextOptions()));
        Assert.AreEqual("a=0Ab", QuotedPrintable.Encode(Ascii("a\nb"), TextOptions()));
    }

    /// <summary>
    /// Verifies that binary mode escapes CR and LF as <c>=0D</c> and <c>=0A</c>.
    /// </summary>
    [TestMethod]
    public void Encode_WhenBinaryModeAndCrLf_ShouldEscape()
    {
        Assert.AreEqual("=0D=0A", QuotedPrintable.Encode(new byte[] { 0x0D, 0x0A }));
    }

    /// <summary>
    /// Verifies that text mode does not emit a literal trailing space before a hard line break.
    /// </summary>
    [TestMethod]
    public void Encode_WhenTextModeAndSpaceBeforeHardBreak_ShouldEscapeSpace()
    {
        string encoded = QuotedPrintable.Encode(Ascii("a \r\nb"), TextOptions());

        Assert.AreEqual("a=20\r\nb", encoded);
    }

    /// <summary>
    /// Verifies that empty input encodes to an empty string.
    /// </summary>
    [TestMethod]
    public void Encode_WhenEmpty_ShouldReturnEmptyString()
    {
        Assert.AreEqual(string.Empty, QuotedPrintable.Encode(ReadOnlySpan<byte>.Empty));
    }

    /// <summary>
    /// Verifies that <see cref="QuotedPrintable.TryEncode(ReadOnlySpan{byte}, Span{char}, out int, QuotedPrintableEncodingOptions)" />
    /// returns <see langword="false" /> and writes zero when the destination is too small.
    /// </summary>
    [TestMethod]
    public void TryEncode_WhenDestinationTooSmall_ShouldReturnFalseAndWriteZero()
    {
        byte[] bytes = new byte[] { 0xFF, 0xFF };
        Span<char> destination = new char[3];

        bool success = QuotedPrintable.TryEncode(bytes, destination, out int charsWritten);

        Assert.IsFalse(success);
        Assert.AreEqual(0, charsWritten);
    }
}
