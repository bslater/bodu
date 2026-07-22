// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Rfc4180Corpus.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Test.Kat;
using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains the RFC 4180 conformance corpus for <see cref="Utf8DelimitedReader" /> — a curated sweep of the quoting,
/// line-ending, empty-field, and embedded-content cases exercised by the community <c>csv-spectrum</c> corpus and the
/// RFC 4180 grammar, run in the Regression tier.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Gets the corpus rows: each input document (parsed positionally) and its expected decoded records.
    /// </summary>
    public static IEnumerable<object[]> Rfc4180CorpusData
    {
        get
        {
            var rows = new List<ValidKat<string, string[][]>>
            {
                new("simple LF rows", "a,b,c\n1,2,3\n", [["a", "b", "c"], ["1", "2", "3"]]),
                new("CRLF rows", "a,b,c\r\n1,2,3\r\n", [["a", "b", "c"], ["1", "2", "3"]]),
                new("no trailing newline", "a,b,c\n1,2,3", [["a", "b", "c"], ["1", "2", "3"]]),
                new("comma inside quotes", "John,\"Anytown, WW\",08123\n", [["John", "Anytown, WW", "08123"]]),
                new("escaped quotes", "1,\"ha \"\"ha\"\" ha\"\n", [["1", "ha \"ha\" ha"]]),
                new("newline inside quotes", "1,\"line one\nline two\",3\n", [["1", "line one\nline two", "3"]]),
                new("CRLF inside quotes", "1,\"line one\r\nline two\",3\n", [["1", "line one\r\nline two", "3"]]),
                new("quoted empty fields", "1,\"\",\"\"\n2,3,4\n", [["1", string.Empty, string.Empty], ["2", "3", "4"]]),
                new("unquoted empty fields", "1,,3\n", [["1", string.Empty, "3"]]),
                new("trailing comma yields empty last field", "1,2,\n", [["1", "2", string.Empty]]),
                new("json inside quotes", "1,\"{\"\"type\"\": \"\"Point\"\"}\"\n", [["1", "{\"type\": \"Point\"}"]]),
                new("utf8 multibyte fields", "café,naïve\n☃,👍\n", [["café", "naïve"], ["☃", "👍"]]),
                new("quoted first and last fields", "\"a\",b,\"c\"\n", [["a", "b", "c"]]),
                new("unquoted spaces preserved", "a, b ,c\n", [["a", " b ", "c"]]),
                new("single column", "a\nb\nc\n", [["a"], ["b"], ["c"]]),
                new("quoted field holding only quotes", "\"\"\"\"\n", [["\""]]),
                new("delimiter-heavy quoted field", "\",,,\",x\n", [[",,,", "x"]]),
                new("mixed LF and CRLF endings", "a,b\r\nc,d\ne,f\r\n", [["a", "b"], ["c", "d"], ["e", "f"]]),
            };

            foreach (ValidKat<string, string[][]> row in rows)
                yield return new object[] { row };
        }
    }

    /// <summary>
    /// Verifies that each corpus document decodes to exactly its expected records and fields when parsed positionally.
    /// </summary>
    /// <param name="kat">The corpus row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(Rfc4180CorpusData),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Read_WhenRfc4180CorpusDocument_ShouldProduceExpectedRows(ValidKat<string, string[][]> kat)
    {
        var reader = new Utf8DelimitedReader(
            Encoding.UTF8.GetBytes(kat.Input),
            new DelimitedReaderOptions { NoHeader = true });

        var records = new List<string[]>();
        List<string>? fields = null;
        int depth = 0;

        while (reader.Read())
        {
            switch (reader.TokenType)
            {
                case DelimitedTokenType.StartArray:
                case DelimitedTokenType.StartObject:
                    depth++;
                    if (depth == 2)
                        fields = [];
                    break;

                case DelimitedTokenType.String:
                    fields?.Add(reader.GetString());
                    break;

                case DelimitedTokenType.EndArray:
                case DelimitedTokenType.EndObject:
                    if (depth == 2)
                    {
                        records.Add([.. fields!]);
                        fields = null;
                    }

                    depth--;
                    break;

                default:
                    break;
            }
        }

        Assert.AreEqual(kat.Expected.Length, records.Count, "record count");
        for (int i = 0; i < records.Count; i++)
            CollectionAssert.AreEqual(kat.Expected[i], records[i], $"record {i}");
    }
}
