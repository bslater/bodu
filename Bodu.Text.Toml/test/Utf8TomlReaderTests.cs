// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8TomlReaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the behaviour of <see cref="Utf8TomlReader" />, the source-order UTF-8 TOML lexer: its token vocabulary,
/// lexical validation, and lazy decoding.
/// </summary>
[TestClass]
public sealed partial class Utf8TomlReaderTests
{
    /// <summary>
    /// Verifies that a key/value pair lexes as one key segment followed by the value token, in source order.
    /// </summary>
    [TestMethod]
    public void Read_WhenSimpleKeyValue_ShouldEmitKeyThenValue()
    {
        Utf8TomlReader lexer = Create("a = 1\n");

        CollectionAssert.AreEqual(
            new[] { "Key(a)!", "Integer(1)" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that a table header lexes as a header token followed by one key token per dotted segment, with the
    /// final segment flagged.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedTableHeader_ShouldEmitHeaderThenKeySegments()
    {
        Utf8TomlReader lexer = Create("[server.tls]\nenabled = true\n");

        CollectionAssert.AreEqual(
            new[] { "TableHeader", "Key(server)", "Key(tls)!", "Key(enabled)!", "Boolean(true)" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that an array-of-tables header lexes as its own header token followed by its key segments.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayTableHeader_ShouldEmitArrayTableHeaderToken()
    {
        Utf8TomlReader lexer = Create("[[fruit]]\nname = \"apple\"\n");

        CollectionAssert.AreEqual(
            new[] { "ArrayTableHeader", "Key(fruit)!", "Key(name)!", "String(apple)" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that a dotted key lexes as one key token per segment in source order, not as nested tables.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedKey_ShouldEmitOneKeyPerSegment()
    {
        Utf8TomlReader lexer = Create("a.b.c = 1\n");

        CollectionAssert.AreEqual(
            new[] { "Key(a)", "Key(b)", "Key(c)!", "Integer(1)" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that an array value lexes as start/end tokens enclosing each element in source order.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayValue_ShouldEmitStartAndEndArray()
    {
        Utf8TomlReader lexer = Create("ports = [1, 2, 3]\n");

        CollectionAssert.AreEqual(
            new[] { "Key(ports)!", "StartArray", "Integer(1)", "Integer(2)", "Integer(3)", "EndArray" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that an inline table lexes with inline-table tokens distinct from header-defined structure.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTable_ShouldEmitInlineTableTokens()
    {
        Utf8TomlReader lexer = Create("p = { x = 1, y = 2 }\n");

        CollectionAssert.AreEqual(
            new[] { "Key(p)!", "StartInlineTable", "Key(x)!", "Integer(1)", "Key(y)!", "Integer(2)", "EndInlineTable" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that comments are surfaced as tokens carrying the text after the <c>#</c>.
    /// </summary>
    [TestMethod]
    public void Read_WhenComments_ShouldSurfaceCommentTokens()
    {
        Utf8TomlReader lexer = Create("# leading\na = 1 # trailing\n");

        CollectionAssert.AreEqual(
            new[] { "Comment( leading)", "Key(a)!", "Integer(1)", "Comment( trailing)" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that comments inside an array surface as tokens between the element tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenCommentInsideArray_ShouldSurfaceCommentToken()
    {
        Utf8TomlReader lexer = Create("v = [ # first\n1,\n2 ]\n");

        CollectionAssert.AreEqual(
            new[] { "Key(v)!", "StartArray", "Comment( first)", "Integer(1)", "Integer(2)", "EndArray" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that every scalar kind lexes to the matching token type and decoded value.
    /// </summary>
    [TestMethod]
    public void Read_WhenEveryScalarKind_ShouldClassifyAndDecodeEachValue()
    {
        Utf8TomlReader lexer = Create(
            "s = \"str\"\ni = 42\nf = 1.5\nb = true\nodt = 1979-05-27T07:32:00Z\nldt = 1979-05-27T07:32:00\nld = 1979-05-27\nlt = 07:32:00\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "Key(s)!", "String(str)",
                "Key(i)!", "Integer(42)",
                "Key(f)!", "Float(1.5)",
                "Key(b)!", "Boolean(true)",
                "Key(odt)!", "OffsetDateTime(1979-05-27T07:32:00.0000000+00:00)",
                "Key(ldt)!", "LocalDateTime(1979-05-27T07:32:00.0000000)",
                "Key(ld)!", "LocalDate(1979-05-27)",
                "Key(lt)!", "LocalTime(07:32:00.0000000)",
            },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that the lexer accepts structurally invalid input — duplicate keys lex cleanly because duplicate
    /// detection is the document builder's responsibility, not the lexer's.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateKeys_ShouldLexCleanly()
    {
        Utf8TomlReader lexer = Create("a = 1\na = 2\n");

        CollectionAssert.AreEqual(
            new[] { "Key(a)!", "Integer(1)", "Key(a)!", "Integer(2)" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that redefining a table lexes cleanly, because table redefinition is a structural rule enforced by the
    /// document builder.
    /// </summary>
    [TestMethod]
    public void Read_WhenTableRedefined_ShouldLexCleanly()
    {
        Utf8TomlReader lexer = Create("[a]\n[a]\n");

        CollectionAssert.AreEqual(
            new[] { "TableHeader", "Key(a)!", "TableHeader", "Key(a)!" },
            Drain(ref lexer));
    }

    /// <summary>
    /// Verifies that an empty document produces no tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDocument_ShouldProduceNoTokens()
    {
        Utf8TomlReader lexer = Create(string.Empty);

        Assert.IsFalse(lexer.Read());
        Assert.AreEqual(TomlTokenType.None, lexer.TokenType);
    }

    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is skipped.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingByteOrderMark_ShouldSkipIt()
    {
        byte[] source = [0xEF, 0xBB, 0xBF, .. "a = 1\n"u8.ToArray()];
        var lexer = new Utf8TomlReader(source, new TomlReaderOptions { SpecVersion = TomlSpecVersion.V1_0 });

        Assert.IsTrue(lexer.Read());
        Assert.AreEqual(TomlTokenType.Key, lexer.TokenType);
        Assert.AreEqual("a", lexer.GetString());
        Assert.AreEqual(1, lexer.ColumnNumber);
    }

    /// <summary>
    /// Creates a lexer over the UTF-8 encoding of <paramref name="toml" />.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <param name="specVersion">The specification version to enforce.</param>
    /// <returns>A lexer positioned before the first token.</returns>
    private static Utf8TomlReader Create(string toml, TomlSpecVersion specVersion = TomlSpecVersion.V1_0) =>
        new(Encoding.UTF8.GetBytes(toml), new TomlReaderOptions { SpecVersion = specVersion });

    /// <summary>
    /// Drains the lexer to the end of the document, formatting each token as <c>TokenType(value)</c> — with a
    /// trailing <c>!</c> marking a final key segment — so a test can assert the exact source-order token sequence.
    /// </summary>
    /// <param name="lexer">The lexer to drain.</param>
    /// <returns>The formatted token entries in read order.</returns>
    private static List<string> Drain(ref Utf8TomlReader lexer)
    {
        var tokens = new List<string>();
        while (lexer.Read())
            tokens.Add(FormatToken(ref lexer));

        return tokens;
    }

    /// <summary>
    /// Formats the reader's current token as <c>TokenType(value)</c> — with a trailing <c>!</c> marking a final key
    /// segment — for exact token-sequence assertions.
    /// </summary>
    /// <param name="lexer">The reader positioned on the token to format.</param>
    /// <returns>The formatted token entry.</returns>
    private static string FormatToken(ref Utf8TomlReader lexer) =>
        lexer.TokenType switch
        {
            TomlTokenType.Key => $"Key({lexer.GetString()}){(lexer.IsFinalKeySegment ? "!" : string.Empty)}",
            TomlTokenType.String => $"String({lexer.GetString()})",
            TomlTokenType.Comment => $"Comment({lexer.GetString()})",
            TomlTokenType.Integer => $"Integer({lexer.GetInt64().ToString(CultureInfo.InvariantCulture)})",
            TomlTokenType.Float => $"Float({lexer.GetDouble().ToString("R", CultureInfo.InvariantCulture)})",
            TomlTokenType.Boolean => $"Boolean({(lexer.GetBoolean() ? "true" : "false")})",
            TomlTokenType.OffsetDateTime => $"OffsetDateTime({lexer.GetDateTimeOffset().ToString("o", CultureInfo.InvariantCulture)})",
            TomlTokenType.LocalDateTime => $"LocalDateTime({lexer.GetDateTime().ToString("o", CultureInfo.InvariantCulture)})",
            TomlTokenType.LocalDate => $"LocalDate({lexer.GetDateOnly().ToString("o", CultureInfo.InvariantCulture)})",
            TomlTokenType.LocalTime => $"LocalTime({lexer.GetTimeOnly().ToString("o", CultureInfo.InvariantCulture)})",
            _ => lexer.TokenType.ToString(),
        };
}
