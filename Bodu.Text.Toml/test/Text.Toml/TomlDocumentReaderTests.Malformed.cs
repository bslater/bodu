// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.Malformed.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Yields a broad catalogue of malformed TOML documents that the reader must reject by raising
    /// <see cref="TomlFormatException" /> from its constructor.
    /// </summary>
    /// <returns>One <c>[DynamicData]</c> row per malformed-input scenario.</returns>
    /// <remarks>
    /// The catalogue sweeps the grammar dimensions — key/value structure, strings and escapes, numbers, date-times,
    /// tables, arrays, inline tables, and comments — to confirm that each documented rejection rule fires. The rows
    /// are derived from the TOML v1.0.0 grammar.
    /// </remarks>
    public static IEnumerable<object[]> MalformedDocuments()
    {
        static object[] Row(string name, string toml) => [new InvalidKat<string>(name, toml, typeof(TomlFormatException))];

        // Key/value structure.
        yield return Row("key without equals", "a 1\n");
        yield return Row("key without value", "a =\n");
        yield return Row("value without key", "= 1\n");
        yield return Row("bare key with space", "a b = 1\n");
        yield return Row("trailing content after value", "a = 1 2\n");
        yield return Row("duplicate key", "a = 1\na = 2\n");
        yield return Row("duplicate dotted key", "a.b = 1\na.b = 2\n");

        // Strings and escapes.
        yield return Row("unterminated basic string", "a = \"abc\n");
        yield return Row("unterminated literal string", "a = 'abc\n");
        yield return Row("invalid escape", "a = \"\\q\"\n");
        yield return Row("bare backslash", "a = \"\\\"\n");
        yield return Row("short unicode escape truncated", "a = \"\\u12\"\n");
        yield return Row("unicode escape out of range", "a = \"\\UFFFFFFFF\"\n");
        yield return Row("multiline key", "\"\"\"k\"\"\" = 1\n");

        // Numbers.
        yield return Row("integer leading zero", "a = 01\n");
        yield return Row("integer double underscore", "a = 1__2\n");
        yield return Row("integer trailing underscore", "a = 1_\n");
        yield return Row("integer overflow", "a = 9223372036854775808\n");
        yield return Row("hex empty body", "a = 0x\n");
        yield return Row("octal bad digit", "a = 0o8\n");
        yield return Row("float missing integer part", "a = .5\n");
        yield return Row("float missing fraction digits", "a = 5.\n");
        yield return Row("float bad exponent", "a = 1e\n");

        // Date-times.
        yield return Row("month out of range", "a = 2020-13-01\n");
        yield return Row("day out of range", "a = 2020-01-32\n");
        yield return Row("hour out of range", "a = 25:00:00\n");
        yield return Row("minute out of range", "a = 12:60:00\n");
        yield return Row("leap second", "a = 23:59:60\n");
        yield return Row("time without seconds under v1.0", "a = 07:32\n");

        // Booleans.
        yield return Row("capitalized true", "a = True\n");
        yield return Row("capitalized false", "a = False\n");
        yield return Row("yes is not boolean", "a = yes\n");

        // Tables.
        yield return Row("unterminated table header", "[a\n");
        yield return Row("duplicate table", "[a]\n[a]\n");
        yield return Row("table over value", "a = 1\n[a]\n");
        yield return Row("array-of-tables over table", "[a]\n[[a]]\n");
        yield return Row("empty table key", "[]\n");

        // Arrays.
        yield return Row("unterminated array", "a = [1, 2\n");
        yield return Row("array missing comma", "a = [1 2]\n");
        yield return Row("array leading comma", "a = [, 1]\n");

        // Inline tables.
        yield return Row("unterminated inline table", "a = {x = 1\n");
        yield return Row("inline table trailing comma under v1.0", "a = {x = 1,}\n");
        yield return Row("inline table duplicate key", "a = {x = 1, x = 2}\n");

        // Comments and newlines.
        yield return Row("missing newline between pairs", "a = 1 b = 2\n");
    }

    /// <summary>
    /// Verifies that each malformed TOML document in the catalogue is rejected by the constructor with
    /// <see cref="TomlFormatException" />, and that the recorded source location is populated.
    /// </summary>
    /// <param name="kat">The malformed-input scenario under test.</param>
    [TestMethod]
    [DynamicData(nameof(MalformedDocuments), DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    [TestCategory("Regression")]
    public void Constructor_WhenDocumentMalformed_ShouldThrowTomlFormatException(InvalidKat<string> kat)
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new TomlDocumentReader(Encoding.UTF8.GetBytes(kat.Input));
        });

        Assert.AreEqual(kat.ExceptionType, ex.GetType());
        Assert.IsNotNull(ex.LineNumber);
        Assert.IsNotNull(ex.ColumnNumber);
        Assert.IsNotNull(ex.Offset);
    }
}
