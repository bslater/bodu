// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlLexerTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the behaviour of <see cref="TomlLexer" />, the source-order UTF-8 TOML lexer: its token vocabulary,
/// lexical validation, and lazy decoding.
/// </summary>
[TestClass]
public sealed partial class TomlLexerTests
{
    /// <summary>
    /// Verifies that a key/value pair lexes as one key segment followed by the value token, in source order.
    /// </summary>
    [TestMethod]
    public void Read_WhenSimpleKeyValue_ShouldEmitKeyThenValue()
    {
        TomlLexer lexer = CreateLexer("a = 1\n");

        CollectionAssert.AreEqual(
            new[] { "Key(a)!", "Integer(1)" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that a table header lexes as a header token followed by one key token per dotted segment, with the
    /// final segment flagged.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedTableHeader_ShouldEmitHeaderThenKeySegments()
    {
        TomlLexer lexer = CreateLexer("[server.tls]\nenabled = true\n");

        CollectionAssert.AreEqual(
            new[] { "TableHeader", "Key(server)", "Key(tls)!", "Key(enabled)!", "Boolean(true)" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that an array-of-tables header lexes as its own header token followed by its key segments.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayTableHeader_ShouldEmitArrayTableHeaderToken()
    {
        TomlLexer lexer = CreateLexer("[[fruit]]\nname = \"apple\"\n");

        CollectionAssert.AreEqual(
            new[] { "ArrayTableHeader", "Key(fruit)!", "Key(name)!", "String(apple)" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that a dotted key lexes as one key token per segment in source order, not as nested tables.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedKey_ShouldEmitOneKeyPerSegment()
    {
        TomlLexer lexer = CreateLexer("a.b.c = 1\n");

        CollectionAssert.AreEqual(
            new[] { "Key(a)", "Key(b)", "Key(c)!", "Integer(1)" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that an array value lexes as start/end tokens enclosing each element in source order.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayValue_ShouldEmitStartAndEndArray()
    {
        TomlLexer lexer = CreateLexer("ports = [1, 2, 3]\n");

        CollectionAssert.AreEqual(
            new[] { "Key(ports)!", "StartArray", "Integer(1)", "Integer(2)", "Integer(3)", "EndArray" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that an inline table lexes with inline-table tokens distinct from header-defined structure.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTable_ShouldEmitInlineTableTokens()
    {
        TomlLexer lexer = CreateLexer("p = { x = 1, y = 2 }\n");

        CollectionAssert.AreEqual(
            new[] { "Key(p)!", "StartInlineTable", "Key(x)!", "Integer(1)", "Key(y)!", "Integer(2)", "EndInlineTable" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that comments are surfaced as tokens carrying the text after the <c>#</c>.
    /// </summary>
    [TestMethod]
    public void Read_WhenComments_ShouldSurfaceCommentTokens()
    {
        TomlLexer lexer = CreateLexer("# leading\na = 1 # trailing\n");

        CollectionAssert.AreEqual(
            new[] { "Comment( leading)", "Key(a)!", "Integer(1)", "Comment( trailing)" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that comments inside an array surface as tokens between the element tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenCommentInsideArray_ShouldSurfaceCommentToken()
    {
        TomlLexer lexer = CreateLexer("v = [ # first\n1,\n2 ]\n");

        CollectionAssert.AreEqual(
            new[] { "Key(v)!", "StartArray", "Comment( first)", "Integer(1)", "Integer(2)", "EndArray" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that every scalar kind lexes to the matching token type and decoded value.
    /// </summary>
    [TestMethod]
    public void Read_WhenEveryScalarKind_ShouldClassifyAndDecodeEachValue()
    {
        TomlLexer lexer = CreateLexer(
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
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that the lexer accepts structurally invalid input — duplicate keys lex cleanly because duplicate
    /// detection is the document builder's responsibility, not the lexer's.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateKeys_ShouldLexCleanly()
    {
        TomlLexer lexer = CreateLexer("a = 1\na = 2\n");

        CollectionAssert.AreEqual(
            new[] { "Key(a)!", "Integer(1)", "Key(a)!", "Integer(2)" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that redefining a table lexes cleanly, because table redefinition is a structural rule enforced by the
    /// document builder.
    /// </summary>
    [TestMethod]
    public void Read_WhenTableRedefined_ShouldLexCleanly()
    {
        TomlLexer lexer = CreateLexer("[a]\n[a]\n");

        CollectionAssert.AreEqual(
            new[] { "TableHeader", "Key(a)!", "TableHeader", "Key(a)!" },
            DrainLexer(ref lexer));
    }

    /// <summary>
    /// Verifies that an empty document produces no tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDocument_ShouldProduceNoTokens()
    {
        TomlLexer lexer = CreateLexer(string.Empty);

        Assert.IsFalse(lexer.Read());
        Assert.AreEqual(TomlLexTokenType.None, lexer.TokenType);
    }

    /// <summary>
    /// Verifies that a leading UTF-8 byte-order mark is skipped.
    /// </summary>
    [TestMethod]
    public void Read_WhenLeadingByteOrderMark_ShouldSkipIt()
    {
        byte[] source = [0xEF, 0xBB, 0xBF, .. "a = 1\n"u8.ToArray()];
        var lexer = new TomlLexer(source, TomlSpecVersion.V1_0);

        Assert.IsTrue(lexer.Read());
        Assert.AreEqual(TomlLexTokenType.Key, lexer.TokenType);
        Assert.AreEqual("a", lexer.GetString());
        Assert.AreEqual(1, lexer.ColumnNumber);
    }

    /// <summary>
    /// Creates a lexer over the UTF-8 encoding of <paramref name="toml" />.
    /// </summary>
    /// <param name="toml">The TOML source text.</param>
    /// <param name="specVersion">The specification version to enforce.</param>
    /// <returns>A lexer positioned before the first token.</returns>
    private static TomlLexer CreateLexer(string toml, TomlSpecVersion specVersion = TomlSpecVersion.V1_0) =>
        new(Encoding.UTF8.GetBytes(toml), specVersion);

    /// <summary>
    /// Drains the lexer to the end of the document, formatting each token as <c>TokenType(value)</c> — with a
    /// trailing <c>!</c> marking a final key segment — so a test can assert the exact source-order token sequence.
    /// </summary>
    /// <param name="lexer">The lexer to drain.</param>
    /// <returns>The formatted token entries in read order.</returns>
    private static List<string> DrainLexer(ref TomlLexer lexer)
    {
        var tokens = new List<string>();
        while (lexer.Read())
        {
            var entry = lexer.TokenType switch
            {
                TomlLexTokenType.Key => $"Key({lexer.GetString()}){(lexer.IsFinalKeySegment ? "!" : string.Empty)}",
                TomlLexTokenType.String => $"String({lexer.GetString()})",
                TomlLexTokenType.Comment => $"Comment({lexer.GetString()})",
                TomlLexTokenType.Integer => $"Integer({lexer.GetInt64().ToString(CultureInfo.InvariantCulture)})",
                TomlLexTokenType.Float => $"Float({lexer.GetDouble().ToString("R", CultureInfo.InvariantCulture)})",
                TomlLexTokenType.Boolean => $"Boolean({(lexer.GetBoolean() ? "true" : "false")})",
                TomlLexTokenType.OffsetDateTime => $"OffsetDateTime({lexer.GetDateTimeOffset().ToString("o", CultureInfo.InvariantCulture)})",
                TomlLexTokenType.LocalDateTime => $"LocalDateTime({lexer.GetDateTime().ToString("o", CultureInfo.InvariantCulture)})",
                TomlLexTokenType.LocalDate => $"LocalDate({lexer.GetDateOnly().ToString("o", CultureInfo.InvariantCulture)})",
                TomlLexTokenType.LocalTime => $"LocalTime({lexer.GetTimeOnly().ToString("o", CultureInfo.InvariantCulture)})",
                _ => lexer.TokenType.ToString(),
            };

            tokens.Add(entry);
        }

        return tokens;
    }
}
