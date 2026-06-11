// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the behaviour of <see cref="Utf8TomlReader" />, the forward-only TOML token reader.
/// </summary>
[TestClass]
public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that a representative document can be read happy-path, producing a table containing a scalar.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Read_WhenSimpleKeyValue_ShouldYieldTableWithScalar()
    {
        Utf8TomlReader reader = Create("a = 1\n");

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(TomlTokenType.StartTable, reader.TokenType);

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(TomlTokenType.PropertyName, reader.TokenType);
        Assert.AreEqual("a", reader.GetString());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(TomlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(1L, reader.GetInt64());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(TomlTokenType.EndTable, reader.TokenType);

        Assert.IsFalse(reader.Read());
        Assert.AreEqual(TomlTokenType.None, reader.TokenType);
    }

    /// <summary>
    /// Verifies that each scalar value kind is surfaced as the matching token type and decoded value.
    /// </summary>
    [TestMethod]
    public void Read_WhenScalarKeyValues_ShouldDecodeEachValue()
    {
        Utf8TomlReader reader = Create("i = 42\ns = \"x\"\nf = 1.5\nb = true\n");

        ExpectStartTable(ref reader);

        ExpectProperty(ref reader, "i");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(42L, reader.GetInt64());

        ExpectProperty(ref reader, "s");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("x", reader.GetString());

        ExpectProperty(ref reader, "f");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(1.5d, reader.GetDouble());

        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.Boolean);
        Assert.IsTrue(reader.GetBoolean());

        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a <c>[table]</c> header surfaces as a nested table reached through its property name.
    /// </summary>
    [TestMethod]
    public void Read_WhenTableHeader_ShouldNestKeysUnderTable()
    {
        Utf8TomlReader reader = Create("[owner]\nname = \"Tom\"\nage = 45\n");

        ExpectStartTable(ref reader);

        ExpectProperty(ref reader, "owner");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        Assert.AreEqual(1, reader.CurrentDepth);

        ExpectProperty(ref reader, "name");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("Tom", reader.GetString());

        ExpectProperty(ref reader, "age");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(45L, reader.GetInt64());

        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that out-of-line <c>[a.b]</c> headers merge into the correct nested tables.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedHeaderPath_ShouldMergeIntoNestedTables()
    {
        Utf8TomlReader reader = Create("[a.b]\nc = 1\n");

        ExpectStartTable(ref reader);

        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);

        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.StartTable);

        ExpectProperty(ref reader, "c");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());

        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that a dotted key surfaces as nested tables identical to a header path.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedKey_ShouldSurfaceAsNestedTable()
    {
        Utf8TomlReader reader = Create("a.b.c = 1\n");

        ExpectStartTable(ref reader);

        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.StartTable);

        ExpectProperty(ref reader, "c");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());

        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectToken(ref reader, TomlTokenType.EndTable);
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an array surfaces as a start/end array boundary enclosing each element.
    /// </summary>
    [TestMethod]
    public void Read_WhenArray_ShouldSurfaceElementsBetweenArrayBoundaries()
    {
        Utf8TomlReader reader = Create("v = [1, 2, 3]\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "v");

        ExpectToken(ref reader, TomlTokenType.StartArray);
        Assert.AreEqual(1, reader.CurrentDepth);

        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(3L, reader.GetInt64());

        ExpectToken(ref reader, TomlTokenType.EndArray);
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an inline table surfaces identically to a header-defined table.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTable_ShouldSurfaceAsTable()
    {
        Utf8TomlReader reader = Create("p = {x = 1}\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "p");

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an array-of-tables surfaces as an array whose elements are each a table.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayOfTables_ShouldSurfaceArrayOfTables()
    {
        Utf8TomlReader reader = Create("[[products]]\nname = \"A\"\n\n[[products]]\nname = \"B\"\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "products");
        ExpectToken(ref reader, TomlTokenType.StartArray);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "name");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("A", reader.GetString());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "name");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("B", reader.GetString());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.EndArray);
        ExpectEndTable(ref reader);
        Assert.IsFalse(reader.Read());
    }

    /// <summary>
    /// Verifies that an offset date-time is decoded to the expected <see cref="DateTimeOffset" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenOffsetDateTime_ShouldDecodeDateTimeOffset()
    {
        Utf8TomlReader reader = Create("d = 1979-05-27T07:32:00Z\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "d");
        ExpectToken(ref reader, TomlTokenType.OffsetDateTime);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 7, 32, 0, TimeSpan.Zero), reader.GetDateTimeOffset());
    }

    /// <summary>
    /// Verifies that a local date-time is decoded to an unspecified-kind <see cref="DateTime" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenLocalDateTime_ShouldDecodeDateTime()
    {
        Utf8TomlReader reader = Create("d = 1979-05-27T07:32:00\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "d");
        ExpectToken(ref reader, TomlTokenType.LocalDateTime);
        Assert.AreEqual(new DateTime(1979, 5, 27, 7, 32, 0, DateTimeKind.Unspecified), reader.GetDateTime());
        Assert.AreEqual(DateTimeKind.Unspecified, reader.GetDateTime().Kind);
    }

    /// <summary>
    /// Verifies that a local date is decoded to the expected <see cref="DateOnly" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenLocalDate_ShouldDecodeDateOnly()
    {
        Utf8TomlReader reader = Create("d = 1979-05-27\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "d");
        ExpectToken(ref reader, TomlTokenType.LocalDate);
        Assert.AreEqual(new DateOnly(1979, 5, 27), reader.GetDateOnly());
    }

    /// <summary>
    /// Verifies that a local time is decoded to the expected <see cref="TimeOnly" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenLocalTime_ShouldDecodeTimeOnly()
    {
        Utf8TomlReader reader = Create("t = 07:32:10\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "t");
        ExpectToken(ref reader, TomlTokenType.LocalTime);
        Assert.AreEqual(new TimeOnly(7, 32, 10), reader.GetTimeOnly());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.Skip" /> over a table start advances to just past its matching end.
    /// </summary>
    [TestMethod]
    public void Skip_WhenPositionedOnStartTable_ShouldAdvancePastSubtree()
    {
        Utf8TomlReader reader = Create("[owner]\nname = \"Tom\"\nnested.k = 1\n\n[other]\nx = 9\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "owner");
        ExpectToken(ref reader, TomlTokenType.StartTable);

        reader.Skip();
        Assert.AreEqual(TomlTokenType.EndTable, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);

        // The next property after skipping the owner subtree is the second top-level table.
        ExpectProperty(ref reader, "other");
        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(9L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.Skip" /> over an array start advances to just past its matching end.
    /// </summary>
    [TestMethod]
    public void Skip_WhenPositionedOnStartArray_ShouldAdvancePastArray()
    {
        Utf8TomlReader reader = Create("v = [1, [2, 3], 4]\nw = 5\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, TomlTokenType.StartArray);

        reader.Skip();
        Assert.AreEqual(TomlTokenType.EndArray, reader.TokenType);
        Assert.AreEqual(0, reader.CurrentDepth);

        ExpectProperty(ref reader, "w");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(5L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.Skip" /> over a property name advances past the property's value,
    /// matching the <c>Utf8JsonReader.Skip</c> contract.
    /// </summary>
    [TestMethod]
    public void Skip_WhenPositionedOnPropertyName_ShouldSkipPropertyValue()
    {
        Utf8TomlReader reader = Create("v = [1, 2, 3]\nw = 5\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "v");

        reader.Skip();
        Assert.AreEqual(TomlTokenType.EndArray, reader.TokenType);

        ExpectProperty(ref reader, "w");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(5L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that <see cref="Utf8TomlReader.Skip" /> is a no-op when the reader is on a scalar token.
    /// </summary>
    [TestMethod]
    public void Skip_WhenPositionedOnScalar_ShouldNotAdvance()
    {
        Utf8TomlReader reader = Create("a = 1\nb = 2\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "a");
        ExpectToken(ref reader, TomlTokenType.Integer);

        reader.Skip();
        Assert.AreEqual(TomlTokenType.Integer, reader.TokenType);
        Assert.AreEqual(1L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that requesting a value of the wrong kind throws <see cref="InvalidOperationException" />.
    /// </summary>
    [TestMethod]
    public void GetInt64_WhenTokenIsString_ShouldThrowInvalidOperationException()
    {
        // A ref struct cannot be captured by a lambda, so the reader is built and advanced inside the assertion.
        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            Utf8TomlReader reader = Create("s = \"x\"\n");
            ExpectStartTable(ref reader);
            ExpectProperty(ref reader, "s");
            ExpectToken(ref reader, TomlTokenType.String);
            _ = reader.GetInt64();
        });
    }

    /// <summary>
    /// Verifies that a key/value pair without a value throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenValueMissing_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = \n");
        });
    }

    /// <summary>
    /// Verifies that an unterminated string throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenStringUnterminated_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = \"unterminated\n");
        });
    }

    /// <summary>
    /// Verifies that a duplicate key throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenKeyDuplicated_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = 1\na = 2\n");
        });
    }

    /// <summary>
    /// Verifies that a malformed date throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenDateInvalid_ShouldThrowTomlFormatException()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("d = 2020-13-01\n");
        });
    }

    /// <summary>
    /// Verifies that the recorded line, column, and offset are populated on a parse failure.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenInvalid_ShouldRecordSourceLocation()
    {
        TomlFormatException ex = Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("a = 1\nb = \n");
        });

        Assert.AreEqual(2, ex.LineNumber);
        Assert.IsNotNull(ex.ColumnNumber);
        Assert.IsNotNull(ex.Offset);
    }

    /// <summary>
    /// Verifies that exceeding the configured maximum depth throws <see cref="TomlFormatException" />.
    /// </summary>
    [TestMethod]
    public void Constructor_WhenNestingExceedsMaxDepth_ShouldThrowTomlFormatException()
    {
        TomlReaderOptions options = new() { MaxDepth = 2 };

        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = new Utf8TomlReader(Encoding.UTF8.GetBytes("v = [[[1]]]\n"), options);
        });
    }

    /// <summary>
    /// Verifies that decimal, hexadecimal, octal, and binary integer literals all decode to their numeric value.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenRadixIntegers_ShouldDecodeToValue()
    {
        Utf8TomlReader reader = Create("d = 1_000\nh = 0xFF\no = 0o17\nb = 0b1010\n");

        ExpectStartTable(ref reader);

        ExpectProperty(ref reader, "d");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1000L, reader.GetInt64());

        ExpectProperty(ref reader, "h");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(255L, reader.GetInt64());

        ExpectProperty(ref reader, "o");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(15L, reader.GetInt64());

        ExpectProperty(ref reader, "b");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(10L, reader.GetInt64());
    }

    /// <summary>
    /// Verifies that the floating-point sentinels <c>inf</c> and <c>nan</c> decode to their IEEE 754 representations.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenFloatSentinels_ShouldDecodeInfinityAndNaN()
    {
        Utf8TomlReader reader = Create("p = inf\nn = -inf\nx = nan\n");

        ExpectStartTable(ref reader);

        ExpectProperty(ref reader, "p");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(double.PositiveInfinity, reader.GetDouble());

        ExpectProperty(ref reader, "n");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.AreEqual(double.NegativeInfinity, reader.GetDouble());

        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Float);
        Assert.IsTrue(double.IsNaN(reader.GetDouble()));
    }

    /// <summary>
    /// Verifies that a multi-line basic string with an escaped newline decodes its content.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenMultilineBasicString_ShouldDecodeContent()
    {
        Utf8TomlReader reader = Create("s = \"\"\"a\nb\"\"\"\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "s");
        ExpectToken(ref reader, TomlTokenType.String);
        Assert.AreEqual("a\nb", reader.GetString());
    }

    /// <summary>
    /// Verifies that an offset date-time with a negative offset decodes to the expected instant.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenOffsetDateTimeNegativeOffset_ShouldDecodeInstant()
    {
        Utf8TomlReader reader = Create("d = 1979-05-27T00:32:00-07:00\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "d");
        ExpectToken(ref reader, TomlTokenType.OffsetDateTime);
        Assert.AreEqual(new DateTimeOffset(1979, 5, 27, 0, 32, 0, TimeSpan.FromHours(-7)), reader.GetDateTimeOffset());
    }

    /// <summary>
    /// Verifies that an array of inline tables surfaces each element as a nested table.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenArrayOfInlineTables_ShouldSurfaceNestedTables()
    {
        Utf8TomlReader reader = Create("v = [{x = 1}, {x = 2}]\n");

        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, TomlTokenType.StartArray);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(1L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.StartTable);
        ExpectProperty(ref reader, "x");
        ExpectToken(ref reader, TomlTokenType.Integer);
        Assert.AreEqual(2L, reader.GetInt64());
        ExpectToken(ref reader, TomlTokenType.EndTable);

        ExpectToken(ref reader, TomlTokenType.EndArray);
        ExpectEndTable(ref reader);
    }

    /// <summary>
    /// Verifies that a time value without seconds is rejected under TOML v1.0.0 but accepted under v1.1.0.
    /// </summary>
    [TestMethod]
    public void Read_WhenTimeWithoutSeconds_ShouldHonorSpecVersion()
    {
        Assert.ThrowsExactly<TomlFormatException>(() =>
        {
            _ = Create("t = 07:32\n");
        });

        TomlReaderOptions options = new() { SpecVersion = TomlSpecVersion.V1_1 };
        Utf8TomlReader reader = new(Encoding.UTF8.GetBytes("t = 07:32\n"), options);
        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "t");
        ExpectToken(ref reader, TomlTokenType.LocalTime);
        Assert.AreEqual(new TimeOnly(7, 32, 0), reader.GetTimeOnly());
    }

    /// <summary>
    /// Creates a reader over the UTF-8 encoding of <paramref name="toml" /> using the default options.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <returns>A reader positioned before the first token.</returns>
    private static Utf8TomlReader Create(string toml) =>
        new(Encoding.UTF8.GetBytes(toml));

    /// <summary>
    /// Creates a reader over the UTF-8 encoding of <paramref name="toml" /> enforcing the TOML v1.1.0 grammar.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <returns>A reader positioned before the first token.</returns>
    private static Utf8TomlReader CreateV11(string toml) =>
        new(Encoding.UTF8.GetBytes(toml), new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_1 });

    /// <summary>
    /// Advances a reader positioned at the document start over <c>StartTable</c> and the single property named
    /// <c>v</c>, then over the value token, asserting it is of the expected kind.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="expected">The expected value token type.</param>
    /// <remarks>
    /// The helper supports the many single key/value tests whose source is <c>v = &lt;value&gt;</c>, leaving the reader
    /// positioned on the decoded value token ready for a typed accessor assertion.
    /// </remarks>
    private static void ExpectSingleValue(ref Utf8TomlReader reader, TomlTokenType expected)
    {
        ExpectStartTable(ref reader);
        ExpectProperty(ref reader, "v");
        ExpectToken(ref reader, expected);
    }

    /// <summary>
    /// Advances the reader and asserts that it landed on a <see cref="TomlTokenType.StartTable" />.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    private static void ExpectStartTable(ref Utf8TomlReader reader) =>
        ExpectToken(ref reader, TomlTokenType.StartTable);

    /// <summary>
    /// Advances the reader and asserts that it landed on a <see cref="TomlTokenType.EndTable" />.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    private static void ExpectEndTable(ref Utf8TomlReader reader) =>
        ExpectToken(ref reader, TomlTokenType.EndTable);

    /// <summary>
    /// Advances the reader and asserts that it landed on a <see cref="TomlTokenType.PropertyName" /> carrying the
    /// expected key.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="expected">The expected property name.</param>
    private static void ExpectProperty(ref Utf8TomlReader reader, string expected)
    {
        ExpectToken(ref reader, TomlTokenType.PropertyName);
        Assert.AreEqual(expected, reader.GetString());
    }

    /// <summary>
    /// Advances the reader and asserts that it landed on the expected token type.
    /// </summary>
    /// <param name="reader">The reader to advance.</param>
    /// <param name="expected">The expected token type.</param>
    private static void ExpectToken(ref Utf8TomlReader reader, TomlTokenType expected)
    {
        Assert.IsTrue(reader.Read(), $"Expected {expected} but the reader reported end of document.");
        Assert.AreEqual(expected, reader.TokenType);
    }
}
