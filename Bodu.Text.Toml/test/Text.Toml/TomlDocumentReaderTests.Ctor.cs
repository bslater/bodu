// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.Ctor.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="TomlDocumentReader" /> constructor, which reads the whole document: where the
/// <see cref="TomlFormatException" /> it throws for a malformed document says the error is (its line, its column and its
/// byte offset), and that the values it reads do not depend on the current culture.
/// </summary>
public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that a key defined a second time is reported at the definition that repeats it: a repeated key and a
    /// repeated dotted key at the first column of the repeating line, and a repeated table header on its own line.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenKeyIsRedefined_ShouldReportTheRedefinition()
    {
        TomlFormatException repeatedKey = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = 1\nb = 2\nb = 3\n");
        });
        TomlFormatException repeatedDottedKey = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("foo.bar = 1\nfoo.bar = 2\n");
        });
        TomlFormatException repeatedHeader = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("[a]\nx = 1\n[a]\ny = 2\n");
        });

        Assert.AreEqual(3, repeatedKey.LineNumber, "The line of the repeated key.");
        Assert.AreEqual(1, repeatedKey.ColumnNumber, "The column of the repeated key.");
        Assert.AreEqual(2, repeatedDottedKey.LineNumber, "The line of the repeated dotted key.");
        Assert.AreEqual(1, repeatedDottedKey.ColumnNumber, "The column of the repeated dotted key.");
        Assert.AreEqual(3, repeatedHeader.LineNumber, "The line of the repeated table header.");
    }

    /// <summary>
    /// Verifies that a key with no equals sign on a last line that has no line feed is reported on that line.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenLastLineHasNoNewline_ShouldReportThatLine()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = 1\nb = 2\nc");
        });

        Assert.AreEqual(3, ex.LineNumber);
    }

    /// <summary>
    /// Verifies that a character outside the bare-key set is reported once, at the first byte of its UTF-8 encoding:
    /// a key spelled U+03BC, and a line that opens with U+3000.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenBareKeyHasMultiByteCharacter_ShouldReportItsFirstByte()
    {
        TomlFormatException greekKey = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("\u03BC = \"greek small letter mu\"\n");
        });
        TomlFormatException ideographicSpace = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = 1\n\u3000foo = \"bar\"\n");
        });

        Assert.AreEqual(1, greekKey.LineNumber, "The line of U+03BC.");
        Assert.AreEqual(1, greekKey.ColumnNumber, "The column of U+03BC.");
        Assert.AreEqual(0, greekKey.Offset, "The offset of U+03BC.");
        Assert.AreEqual(2, ideographicSpace.LineNumber, "The line of U+3000.");
        Assert.AreEqual(1, ideographicSpace.ColumnNumber, "The column of U+3000.");
        Assert.AreEqual(6, ideographicSpace.Offset, "The offset of U+3000.");
    }

    /// <summary>
    /// Verifies that a digit separator directly after a radix prefix is reported at the separator, not at the start of
    /// the value.
    /// </summary>
    /// <param name="toml">The document, whose value puts a separator after a binary, hexadecimal or octal prefix.</param>
    [TestMethod]
    [DataRow("us-after-bin = 0b_1", DisplayName = "binary")]
    [DataRow("us-after-hex = 0x_1", DisplayName = "hexadecimal")]
    [DataRow("us-after-oct = 0o_1", DisplayName = "octal")]
    public void Ctor_WhenDigitSeparatorFollowsRadixPrefix_ShouldReportSeparator(string toml)
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create(toml);
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(18, ex.ColumnNumber);
        Assert.AreEqual(17, ex.Offset);
    }

    /// <summary>
    /// Verifies that a duplicate key in a document with CRLF line endings is reported at the same line and column as in
    /// the same document with LF line endings.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDocumentUsesCrlf_ShouldReportSameLineAndColumnAsLf()
    {
        TomlFormatException crlf = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("\r\n[t1]\r\n[t2]\r\na = 1\r\na = 2\r\n");
        });
        TomlFormatException lf = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("\n[t1]\n[t2]\na = 1\na = 2\n");
        });

        Assert.AreEqual(5, crlf.LineNumber, "The line reported with CRLF line endings.");
        Assert.AreEqual(5, lf.LineNumber, "The line reported with LF line endings.");
        Assert.AreEqual(lf.ColumnNumber, crlf.ColumnNumber, "The column reported with CRLF line endings.");
    }

    /// <summary>
    /// Verifies that a value missing at the end of a line is reported on that line, at its line feed, not at the start
    /// of the next line.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenErrorIsAtEndOfLine_ShouldReportThatLine()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("key =\nnext = 1\n");
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(6, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that an escape whose backslash is followed by a three-byte character is reported at the first byte of
    /// that character, never inside it.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenEscapeIsFollowedByMultiByteCharacter_ShouldReportThatCharacter()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("\"\\\u1F82r\"");
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(3, ex.ColumnNumber);
        Assert.AreEqual(2, ex.Offset);
    }

    /// <summary>
    /// Verifies that a key with no equals sign at the end of the input is reported within the input: on line 1, at an
    /// offset no greater than the input's length, and at the column one past that offset.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenErrorIsAtEndOfInput_ShouldReportPositionWithinInput()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("asdf");
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.IsNotNull(ex.Offset);
        Assert.IsLessThanOrEqualTo(4, ex.Offset.Value);
        Assert.AreEqual(ex.Offset + 1, ex.ColumnNumber);
    }

    /// <summary>
    /// Verifies that a document consisting of one four-byte character is reported at that character's first byte.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenDocumentIsOneMultiByteCharacter_ShouldReportItsFirstByte()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("\U0001F600");
        });

        Assert.AreEqual(1, ex.LineNumber);
        Assert.AreEqual(1, ex.ColumnNumber);
        Assert.AreEqual(0, ex.Offset);
    }

    /// <summary>
    /// Verifies that a float is read, and written back by <see cref="Utf8TomlWriter" />, with a period as its decimal
    /// separator when the current culture uses a comma.
    /// </summary>
    [TestMethod]
    public void Ctor_WhenCurrentCultureUsesCommaDecimalSeparator_ShouldReadFloats()
    {
        CultureInfo original = CultureInfo.CurrentCulture;
        double read;
        var buffer = new ArrayBufferWriter<byte>();
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
            Assert.AreEqual(",", CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator, "The culture under test uses a comma.");

            TomlDocumentReader reader = Create("a = 1.5\n");
            ExpectStartTable(ref reader);
            ExpectProperty(ref reader, "a");
            ExpectToken(ref reader, TomlTokenType.Float);
            read = reader.GetDouble();

            var writer = new Utf8TomlWriter(buffer);
            writer.WriteStartTable();
            writer.WritePropertyName("a");
            writer.WriteFloat(1.5);
            writer.WriteEndTable();
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }

        Assert.AreEqual(1.5, read);
        Assert.AreEqual("a = 1.5\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }
}
