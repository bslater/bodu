// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlDocumentReaderTests.TokenStream.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Text.Toml.Reader;

namespace Bodu.Text.Toml;

public sealed partial class TomlDocumentReaderTests
{
    /// <summary>
    /// Verifies that out-of-line headers merge into nested position, producing the exact normalized token sequence
    /// with a table's entire contents contiguous between its start and end tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenOutOfLineHeaders_ShouldEmitExactMergedTokenSequence()
    {
        TomlDocumentReader reader = Create("[server]\nhost = \"a\"\n\n[client]\ntimeout = 5\n\n[server.tls]\nenabled = true\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "StartTable()@0",
                "PropertyName(server)@0",
                "StartTable()@1",
                "PropertyName(host)@1",
                "String(a)@1",
                "PropertyName(tls)@1",
                "StartTable()@2",
                "PropertyName(enabled)@2",
                "Boolean(true)@2",
                "EndTable()@1",
                "EndTable()@0",
                "PropertyName(client)@0",
                "StartTable()@1",
                "PropertyName(timeout)@1",
                "Integer(5)@1",
                "EndTable()@0",
                "EndTable()@0",
            },
            Drain(ref reader));
    }

    /// <summary>
    /// Verifies that dotted keys produce the exact nested-table token sequence, with sibling dotted keys merged into
    /// one table.
    /// </summary>
    [TestMethod]
    public void Read_WhenDottedKeys_ShouldEmitExactNestedTokenSequence()
    {
        TomlDocumentReader reader = Create("a.b.c = 1\na.b.d = 2\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "StartTable()@0",
                "PropertyName(a)@0",
                "StartTable()@1",
                "PropertyName(b)@1",
                "StartTable()@2",
                "PropertyName(c)@2",
                "Integer(1)@2",
                "PropertyName(d)@2",
                "Integer(2)@2",
                "EndTable()@1",
                "EndTable()@0",
                "EndTable()@0",
            },
            Drain(ref reader));
    }

    /// <summary>
    /// Verifies that an array of tables with an interleaved sub-table header produces the exact token sequence, with
    /// the sub-table nested inside the first array element.
    /// </summary>
    [TestMethod]
    public void Read_WhenArrayOfTablesWithSubTable_ShouldEmitExactTokenSequence()
    {
        TomlDocumentReader reader = Create("[[fruit]]\nname = \"apple\"\n\n[fruit.physical]\ncolor = \"red\"\n\n[[fruit]]\nname = \"banana\"\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "StartTable()@0",
                "PropertyName(fruit)@0",
                "StartArray()@1",
                "StartTable()@2",
                "PropertyName(name)@2",
                "String(apple)@2",
                "PropertyName(physical)@2",
                "StartTable()@3",
                "PropertyName(color)@3",
                "String(red)@3",
                "EndTable()@2",
                "EndTable()@1",
                "StartTable()@2",
                "PropertyName(name)@2",
                "String(banana)@2",
                "EndTable()@1",
                "EndArray()@0",
                "EndTable()@0",
            },
            Drain(ref reader));
    }

    /// <summary>
    /// Verifies that an inline table containing a nested array produces the exact token sequence, indistinguishable
    /// from header-defined structure.
    /// </summary>
    [TestMethod]
    public void Read_WhenInlineTableWithArray_ShouldEmitExactTokenSequence()
    {
        TomlDocumentReader reader = Create("p = { x = 1, y = [\"a\", \"b\"] }\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "StartTable()@0",
                "PropertyName(p)@0",
                "StartTable()@1",
                "PropertyName(x)@1",
                "Integer(1)@1",
                "PropertyName(y)@1",
                "StartArray()@2",
                "String(a)@2",
                "String(b)@2",
                "EndArray()@1",
                "EndTable()@0",
                "EndTable()@0",
            },
            Drain(ref reader));
    }

    /// <summary>
    /// Verifies that every scalar kind surfaces with its exact token type and decoded value in document order.
    /// </summary>
    [TestMethod]
    public void Read_WhenEveryScalarKind_ShouldEmitExactTokenSequence()
    {
        TomlDocumentReader reader = Create(
            "s = \"str\"\ni = 42\nf = 1.5\nb = true\nodt = 1979-05-27T07:32:00Z\nldt = 1979-05-27T07:32:00\nld = 1979-05-27\nlt = 07:32:00\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "StartTable()@0",
                "PropertyName(s)@0",
                "String(str)@0",
                "PropertyName(i)@0",
                "Integer(42)@0",
                "PropertyName(f)@0",
                "Float(1.5)@0",
                "PropertyName(b)@0",
                "Boolean(true)@0",
                "PropertyName(odt)@0",
                "OffsetDateTime(1979-05-27T07:32:00.0000000+00:00)@0",
                "PropertyName(ldt)@0",
                "LocalDateTime(1979-05-27T07:32:00.0000000)@0",
                "PropertyName(ld)@0",
                "LocalDate(1979-05-27)@0",
                "PropertyName(lt)@0",
                "LocalTime(07:32:00.0000000)@0",
                "EndTable()@0",
            },
            Drain(ref reader));
    }

    /// <summary>
    /// Verifies that an empty document produces exactly the root table's start and end tokens.
    /// </summary>
    [TestMethod]
    public void Read_WhenEmptyDocument_ShouldEmitRootTableOnly()
    {
        TomlDocumentReader reader = Create(string.Empty);

        CollectionAssert.AreEqual(
            new[] { "StartTable()@0", "EndTable()@0" },
            Drain(ref reader));
    }

    /// <summary>
    /// Verifies that nested arrays report the exact depth at each token.
    /// </summary>
    [TestMethod]
    public void Read_WhenNestedArrays_ShouldEmitExactDepths()
    {
        TomlDocumentReader reader = Create("v = [[[1]]]\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "StartTable()@0",
                "PropertyName(v)@0",
                "StartArray()@1",
                "StartArray()@2",
                "StartArray()@3",
                "Integer(1)@3",
                "EndArray()@2",
                "EndArray()@1",
                "EndArray()@0",
                "EndTable()@0",
            },
            Drain(ref reader));
    }

    /// <summary>
    /// Verifies that a super-table header defined after its sub-table surfaces the sub-table first and the promoted
    /// key second, in insertion order.
    /// </summary>
    [TestMethod]
    public void Read_WhenSuperTableDefinedAfterSubTable_ShouldEmitExactTokenSequence()
    {
        TomlDocumentReader reader = Create("[a.b]\nx = 1\n\n[a]\ny = 2\n");

        CollectionAssert.AreEqual(
            new[]
            {
                "StartTable()@0",
                "PropertyName(a)@0",
                "StartTable()@1",
                "PropertyName(b)@1",
                "StartTable()@2",
                "PropertyName(x)@2",
                "Integer(1)@2",
                "EndTable()@1",
                "PropertyName(y)@1",
                "Integer(2)@1",
                "EndTable()@0",
                "EndTable()@0",
            },
            Drain(ref reader));
    }

    /// <summary>
    /// Drains the reader to the end of the document, formatting each token as
    /// <c>TokenType(value)@depth</c> so a test can assert the exact normalized token sequence.
    /// </summary>
    /// <param name="reader">The reader to drain.</param>
    /// <returns>The formatted token entries in read order.</returns>
    private static List<string> Drain(ref TomlDocumentReader reader)
    {
        var tokens = new List<string>();
        while (reader.Read())
        {
            string value = reader.TokenType switch
            {
                TomlTokenType.PropertyName or TomlTokenType.String => reader.GetString(),
                TomlTokenType.Integer => reader.GetInt64().ToString(CultureInfo.InvariantCulture),
                TomlTokenType.Float => reader.GetDouble().ToString("R", CultureInfo.InvariantCulture),
                TomlTokenType.Boolean => reader.GetBoolean() ? "true" : "false",
                TomlTokenType.OffsetDateTime => reader.GetDateTimeOffset().ToString("o", CultureInfo.InvariantCulture),
                TomlTokenType.LocalDateTime => reader.GetDateTime().ToString("o", CultureInfo.InvariantCulture),
                TomlTokenType.LocalDate => reader.GetDateOnly().ToString("o", CultureInfo.InvariantCulture),
                TomlTokenType.LocalTime => reader.GetTimeOnly().ToString("o", CultureInfo.InvariantCulture),
                _ => string.Empty,
            };

            tokens.Add($"{reader.TokenType}({value})@{reader.CurrentDepth}");
        }

        return tokens;
    }
}
