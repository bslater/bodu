// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DotEnvReaderTests.DotEnvCorpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Kat;
using Bodu.Text.DotEnv.Reader;

namespace Bodu.Text.DotEnv.Reader;

/// <summary>
/// Contains the DotEnv conformance corpus for <see cref="Utf8DotEnvReader" /> — a curated sweep of the quoting,
/// escape, inline-comment, export, and line-ending cases exercised by the mainstream <c>python-dotenv</c> and
/// <c>godotenv</c> fixture suites, adapted to this reader's documented dialect and run in the Regression tier.
/// </summary>
public partial class Utf8DotEnvReaderTests
{
    /// <summary>
    /// Gets the valid corpus rows: each input document and its expected decoded <c>{key, value}</c> pairs in source
    /// order.
    /// </summary>
    public static IEnumerable<object[]> DotEnvCorpusData
    {
        get
        {
            var rows = new List<ValidKat<string, string[][]>>
            {
                new("simple assignment", "BASIC=basic\n", [["BASIC", "basic"]]),
                new("empty value", "EMPTY=\n", [["EMPTY", string.Empty]]),
                new("quoted empty value", "EMPTY=\"\"\n", [["EMPTY", string.Empty]]),
                new("unquoted trims whitespace", "FOO=  some value  \n", [["FOO", "some value"]]),
                new("whitespace around assignment", "FOO = bar\n", [["FOO", "bar"]]),
                new("export prefix", "export EXPORTED=1\n", [["EXPORTED", "1"]]),
                new("export with tab", "export\tTABBED=1\n", [["TABBED", "1"]]),
                new("double quotes preserve spaces", "SPACED=\"  spaced  \"\n", [["SPACED", "  spaced  "]]),
                new("single quotes preserve spaces", "SPACED='  spaced  '\n", [["SPACED", "  spaced  "]]),
                new("double-quoted escaped newline", "NL=\"line1\\nline2\"\n", [["NL", "line1\nline2"]]),
                new("double-quoted escaped tab and cr", "WS=\"a\\tb\\rc\"\n", [["WS", "a\tb\rc"]]),
                new("double-quoted escaped quote", "Q=\"say \\\"hi\\\"\"\n", [["Q", "say \"hi\""]]),
                new("double-quoted escaped backslash", "BS=\"c:\\\\temp\"\n", [["BS", "c:\\temp"]]),
                new("double-quoted escaped dollar", "DOLLAR=\"\\$HOME\"\n", [["DOLLAR", "$HOME"]]),
                new("unknown escape kept literally", "UNK=\"a\\zb\"\n", [["UNK", "a\\zb"]]),
                new("single quotes keep escapes literal", "RAW='a\\nb'\n", [["RAW", "a\\nb"]]),
                new("multiline double-quoted value", "ML=\"line one\nline two\"\n", [["ML", "line one\nline two"]]),
                new("line continuation in double quotes", "LC=\"one\\\ntwo\"\n", [["LC", "onetwo"]]),
                new("inline comment after unquoted", "IC=value # note\n", [["IC", "value"]]),
                new("hash glued to text is content", "URL=http://host/#anchor\n", [["URL", "http://host/#anchor"]]),
                new("inline comment after quoted value", "QC=\"value\" # note\n", [["QC", "value"]]),
                new("value containing equals", "EQ=a=b=c\n", [["EQ", "a=b=c"]]),
                new("full-line comments and blanks skipped", "# header\n\nA=1\n\n# tail\nB=2\n", [["A", "1"], ["B", "2"]]),
                new("crlf line endings", "A=1\r\nB=2\r\n", [["A", "1"], ["B", "2"]]),
                new("no trailing newline", "A=1", [["A", "1"]]),
                new("underscored and numbered keys", "_KEY_1=a\nKEY_2=b\n", [["_KEY_1", "a"], ["KEY_2", "b"]]),
                new("lowercase keys accepted", "lower=case\n", [["lower", "case"]]),
                new("utf8 multibyte value", "GREETING=\"héllo ☃\"\n", [["GREETING", "héllo ☃"]]),
                new("unquoted utf8 value", "CITY=Zürich\n", [["CITY", "Zürich"]]),
                new("single-quoted hash kept", "SQ='a # not comment'\n", [["SQ", "a # not comment"]]),
                new("last duplicate surfaced in order", "D=1\nD=2\n", [["D", "1"], ["D", "2"]]),
                new("trailing junk after quoted value dropped", "TJ=\"v\"junk\n", [["TJ", "v"]]),
            };

            foreach (ValidKat<string, string[][]> row in rows)
                yield return new object[] { row };
        }
    }

    /// <summary>
    /// Gets the malformed corpus rows: each input document and the exception the reader must throw.
    /// </summary>
    public static IEnumerable<object[]> DotEnvCorpusInvalidData
    {
        get
        {
            var rows = new List<InvalidKat<string>>
            {
                new("unterminated double quote", "A=\"open\n", typeof(DotEnvFormatException)),
                new("unterminated single quote", "A='open\n", typeof(DotEnvFormatException)),
                new("single quote crossing a line", "A='one\ntwo'\n", typeof(DotEnvFormatException)),
                new("missing assignment", "JUSTAKEY\n", typeof(DotEnvFormatException)),
                new("key starting with a digit", "1KEY=v\n", typeof(DotEnvFormatException)),
                new("key starting with a dash", "-KEY=v\n", typeof(DotEnvFormatException)),
                new("escape at end of input", "A=\"x\\", typeof(DotEnvFormatException)),
            };

            foreach (InvalidKat<string> row in rows)
                yield return new object[] { row };
        }
    }

    /// <summary>
    /// Verifies that each corpus document decodes to exactly its expected key/value pairs in source order.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DotEnvCorpusData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenDotEnvCorpusDocument_ShouldProduceExpectedPairs(ValidKat<string, string[][]> kat)
    {
        var reader = new Utf8DotEnvReader(Encoding.UTF8.GetBytes(kat.Input));
        var pairs = new List<string[]>();
        string? key = null;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DotEnvTokenType.PropertyName:
                    key = reader.GetString();
                    break;

                case DotEnvTokenType.String:
                    pairs.Add([key!, reader.GetString()]);
                    key = null;
                    break;

                default:
                    break;
            }
        }

        Assert.AreEqual(kat.Expected.Length, pairs.Count, "pair count");
        for (int i = 0; i < pairs.Count; i++)
            CollectionAssert.AreEqual(kat.Expected[i], pairs[i], $"pair {i}");
    }

    /// <summary>
    /// Verifies that each malformed corpus document throws <see cref="DotEnvFormatException" />.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(DotEnvCorpusInvalidData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenDotEnvCorpusDocumentMalformed_ShouldThrowDotEnvFormatException(InvalidKat<string> kat)
    {
        var exception = Assert.ThrowsExactly<DotEnvFormatException>(() =>
        {
            var reader = new Utf8DotEnvReader(Encoding.UTF8.GetBytes(kat.Input));
            while (reader.Read())
            {
            }
        });

        Assert.IsInstanceOfType(exception, kat.ExceptionType);
    }
}
