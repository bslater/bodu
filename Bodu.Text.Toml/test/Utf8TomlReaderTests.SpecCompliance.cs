// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.SpecCompliance.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Yields documents that are valid under the TOML v1.0.0 grammar and therefore must be accepted, covering
    /// acceptance edge cases not exercised elsewhere in the suite.
    /// </summary>
    /// <returns>One <c>[DynamicData]</c> row per spec-valid scenario.</returns>
    /// <remarks>
    /// The rows pin commonly mishandled acceptance rules: century leap days, CRLF documents, end-of-file without a
    /// trailing newline, a leading byte-order mark, and Unicode escape boundary values immediately adjacent to the
    /// surrogate range and the maximum scalar value.
    /// </remarks>
    public static IEnumerable<object[]> SpecValidDocuments()
    {
        static object[] Row(string name, string toml) => [new ValidKat<string, string>(name, toml, string.Empty)];

        // Date-times.
        yield return Row("century leap day 2000-02-29", "d = 2000-02-29\n");
        yield return Row("standard leap day 2024-02-29", "d = 2024-02-29\n");

        // Newlines and end of file.
        yield return Row("whole document uses CRLF", "a = 1\r\nb = 2\r\n\r\n[t]\r\nc = 3\r\n");
        yield return Row("key/value at EOF without newline", "a = 1");
        yield return Row("comment at EOF without newline", "a = 1\n# done");
        yield return Row("table header at EOF without newline", "[t]");
        yield return Row("empty document", string.Empty);

        // Encoding.
        yield return Row("leading byte-order mark", "\uFEFFa = 1\n");

        // Unicode escape boundaries (valid scalars adjacent to rejected ranges).
        yield return Row("escape below surrogate range", "s = \"\\uD7FF\"\n");
        yield return Row("escape above surrogate range", "s = \"\\uE000\"\n");
        yield return Row("escape at maximum scalar", "s = \"\\U0010FFFF\"\n");

        // Numbers.
        yield return Row("exponent with leading zeros", "f = 1e02\n");
        yield return Row("hex with leading zeros", "i = 0x0000ff\n");
    }

    /// <summary>
    /// Verifies that each spec-valid document in the catalogue is accepted by the reader constructor.
    /// </summary>
    /// <param name="kat">The spec-valid scenario under test.</param>
    [TestMethod]
    [DynamicData(nameof(SpecValidDocuments), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Constructor_WhenDocumentSpecValid_ShouldNotThrow(ValidKat<string, string> kat)
    {
        _ = new Utf8TomlReader(Encoding.UTF8.GetBytes(kat.Input));
    }

    /// <summary>
    /// Yields documents that violate the TOML v1.0.0 grammar and therefore must be rejected, covering rejection edge
    /// cases not exercised elsewhere in the suite.
    /// </summary>
    /// <returns>One <c>[DynamicData]</c> row per spec-invalid scenario.</returns>
    /// <remarks>
    /// The rows pin commonly missed rejection rules: surrogate and out-of-range Unicode escapes, raw control
    /// characters inside every string form, bare carriage returns, signed radix integers, impossible calendar dates,
    /// out-of-range time offsets, duplicate keys spelled bare versus quoted, and structural closedness of scalars,
    /// inline tables, and arrays of tables.
    /// </remarks>
    public static IEnumerable<object[]> SpecInvalidDocuments()
    {
        static object[] Row(string name, string toml) => [new InvalidKat<string>(name, toml, typeof(TomlFormatException))];

        // Unicode escapes.
        yield return Row("short escape low surrogate bound", "s = \"\\uD800\"\n");
        yield return Row("short escape high surrogate bound", "s = \"\\uDFFF\"\n");
        yield return Row("long escape surrogate", "s = \"\\U0000D800\"\n");
        yield return Row("long escape above maximum scalar", "s = \"\\U00110000\"\n");

        // Raw control characters in each string form.
        yield return Row("control char in basic string", "s = \"a\u0001b\"\n");
        yield return Row("delete char in basic string", "s = \"a\u007Fb\"\n");
        yield return Row("control char in literal string", "s = 'a\u0001b'\n");
        yield return Row("control char in multiline basic string", "s = \"\"\"a\u0001b\"\"\"\n");
        yield return Row("control char in multiline literal string", "s = '''a\u0001b'''\n");

        // Bare carriage returns.
        yield return Row("bare CR as line terminator", "a = 1\rb = 2\n");
        yield return Row("bare CR in multiline basic string", "s = \"\"\"a\rb\"\"\"\n");

        // Signed radix integers.
        yield return Row("signed hex integer", "i = +0x1\n");
        yield return Row("signed octal integer", "i = -0o7\n");
        yield return Row("signed binary integer", "i = +0b1\n");

        // Calendar validation.
        yield return Row("day zero", "d = 2020-01-00\n");
        yield return Row("non-leap century day 1900-02-29", "d = 1900-02-29\n");
        yield return Row("non-leap year day 2021-02-29", "d = 2021-02-29\n");

        // Offset ranges.
        yield return Row("offset hour out of range", "d = 1979-05-27T07:32:00+24:00\n");
        yield return Row("offset minute out of range", "d = 1979-05-27T07:32:00+00:60\n");

        // Duplicate keys and structural closedness.
        yield return Row("duplicate key bare then quoted", "a = 1\n\"a\" = 2\n");
        yield return Row("duplicate key quoted then bare", "\"a\" = 1\na = 2\n");
        yield return Row("dotted key through scalar", "a = 1\na.b = 2\n");
        yield return Row("dotted key extends inline table", "a = { b = 1 }\na.c = 2\n");
        yield return Row("table header over array of tables", "[[a]]\n[a]\n");

        // Float exponent edge.
        yield return Row("exponent with sign only", "f = 1e+\n");
    }

    /// <summary>
    /// Verifies that each spec-invalid document in the catalogue is rejected by the constructor with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    /// <param name="kat">The spec-invalid scenario under test.</param>
    [TestMethod]
    [DynamicData(nameof(SpecInvalidDocuments), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Constructor_WhenDocumentSpecInvalid_ShouldThrowTomlFormatException(InvalidKat<string> kat)
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new Utf8TomlReader(Encoding.UTF8.GetBytes(kat.Input));
        });
    }

    /// <summary>
    /// Verifies that a document containing an invalid UTF-8 byte sequence is rejected with
    /// <see cref="TomlFormatException" /> rather than being decoded with replacement characters.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDocumentContainsInvalidUtf8_ShouldThrowTomlFormatException()
    {
        byte[] document = [.. "s = \""u8.ToArray(), 0xFF, .. "\"\n"u8.ToArray()];

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new Utf8TomlReader(document);
        });
    }

    /// <summary>
    /// Verifies that an overlong UTF-8 encoding (a non-shortest-form byte sequence) is rejected with
    /// <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDocumentContainsOverlongUtf8_ShouldThrowTomlFormatException()
    {
        byte[] document = [.. "s = \""u8.ToArray(), 0xC0, 0xAF, .. "\"\n"u8.ToArray()];

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new Utf8TomlReader(document);
        });
    }

    /// <summary>
    /// Verifies that a float literal whose magnitude exceeds <see cref="double.MaxValue" /> decodes to positive
    /// infinity rather than failing, matching the binary64 overflow guidance of the TOML specification.
    /// </summary>
    [TestMethod]
    public void Read_WhenFloatOverflowsDouble_ShouldYieldPositiveInfinity()
    {
        Utf8TomlReader reader = Create("v = 1e400\n");

        ExpectSingleValue(ref reader, TomlTokenType.Float);
        Assert.IsTrue(double.IsPositiveInfinity(reader.GetDouble()));
    }

    /// <summary>
    /// Verifies that a negative-zero float literal preserves its sign bit through decoding.
    /// </summary>
    [TestMethod]
    public void Read_WhenFloatIsNegativeZero_ShouldPreserveSignBit()
    {
        Utf8TomlReader reader = Create("v = -0.0\n");

        ExpectSingleValue(ref reader, TomlTokenType.Float);
        Assert.IsTrue(double.IsNegative(reader.GetDouble()));
    }

    /// <summary>
    /// Verifies that table-header nesting deeper than <see cref="TomlReaderOptions.MaxDepth" /> is rejected with
    /// <see cref="TomlFormatException" />, so that hostile documents cannot drive unbounded recursion.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenHeaderNestingExceedsMaxDepth_ShouldThrowTomlFormatException()
    {
        string header = "[" + string.Join('.', Enumerable.Repeat("a", 300)) + "]\n";

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new Utf8TomlReader(Encoding.UTF8.GetBytes(header));
        });
    }

    /// <summary>
    /// Verifies that dotted-key nesting deeper than <see cref="TomlReaderOptions.MaxDepth" /> is rejected with
    /// <see cref="TomlFormatException" />, so that hostile documents cannot drive unbounded recursion.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDottedKeyNestingExceedsMaxDepth_ShouldThrowTomlFormatException()
    {
        string pair = string.Join('.', Enumerable.Repeat("a", 300)) + " = 1\n";

        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new Utf8TomlReader(Encoding.UTF8.GetBytes(pair));
        });
    }

    /// <summary>
    /// Verifies that under the TOML v1.1.0 grammar a comment may not contain a control character other than tab: the
    /// v1.1.0 draft retains the v1.0.0 prohibition (U+0000–U+0008, U+000A–U+001F, U+007F).
    /// </summary>
    [TestMethod]
    public void Constructor_WhenCommentContainsControlCharacter_ForV11_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = CreateV11("a = 1 # comment \u0001 control\n");
        });
    }

    /// <summary>
    /// Verifies that a table header may not re-open an implicit super-table after the table has been extended via
    /// dotted keys, because the dotted assignment defines the table in dotted-key form.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenHeaderReopensTableExtendedByDottedKeys_ShouldThrowTomlFormatException()
    {
        _ = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new Utf8TomlReader("[x.y.z]\n[x]\ny.q = 1\n[x.y]\n"u8);
        });
    }
}
