// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Policies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

using Bodu.Text.Delimited.Document;
using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Reader;

/// <summary>
/// Contains header-policy and field-count-policy robustness tests for <see cref="Utf8DelimitedReader" />: duplicate
/// header resolution, ragged records, and malformed-record recovery.
/// </summary>
public partial class Utf8DelimitedReaderTests
{
    /// <summary>
    /// Verifies that a duplicate header column throws under the strict default policy.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateHeaderDefault_ShouldThrowDelimitedFormatException()
    {
        Assert.ThrowsExactly<DelimitedFormatException>(() =>
        {
            _ = Transcribe("id,name,id\n1,Ada,2\n");
        });
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDuplicateHeaderBehavior.TakeFirst" /> resolves a duplicated column name to its
    /// first occurrence.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateHeaderTakeFirst_ShouldBindFirstColumn()
    {
        var options = new DelimitedReaderOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeFirst };
        using DelimitedDocument document = DelimitedDocument.Parse("id,name,id\n1,Ada,2\n"u8, options);

        Assert.AreEqual("1", document.RootElement[0].GetProperty("id").GetString());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDuplicateHeaderBehavior.TakeLast" /> resolves a duplicated column name to its
    /// last occurrence.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateHeaderTakeLast_ShouldBindLastColumn()
    {
        var options = new DelimitedReaderOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeLast };
        using DelimitedDocument document = DelimitedDocument.Parse("id,name,id\n1,Ada,2\n"u8, options);

        Assert.AreEqual("2", document.RootElement[0].GetProperty("id").GetString());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDuplicateHeaderBehavior.TakeFirst" /> leaves a duplicated name to its first
    /// column and reports every later column with the name under an empty name, both as the record's property names and
    /// in <see cref="Utf8DelimitedReader.Headers" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateHeaderTakeFirst_ShouldReportLaterColumnsUnderAnEmptyName()
    {
        const string Source = "a,b,a,a\n1,2,3,4\n";
        var options = new DelimitedReaderOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeFirst };

        CollectionAssert.AreEqual(
            new[] { "StartArray", "StartObject", "Name:a", "String:1", "Name:b", "String:2", "Name:", "String:3", "Name:", "String:4", "EndObject", "EndArray" },
            Transcribe(Source, options));
        CollectionAssert.AreEqual(new[] { "a", "b", string.Empty, string.Empty }, ReadHeaders(Source, options));
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDuplicateHeaderBehavior.TakeLast" /> leaves a duplicated name to its last
    /// column and reports every earlier column with the name under an empty name, both as the record's property names
    /// and in <see cref="Utf8DelimitedReader.Headers" />.
    /// </summary>
    [TestMethod]
    public void Read_WhenDuplicateHeaderTakeLast_ShouldReportEarlierColumnsUnderAnEmptyName()
    {
        const string Source = "a,b,a,a\n1,2,3,4\n";
        var options = new DelimitedReaderOptions { DuplicateHeaderBehavior = DelimitedDuplicateHeaderBehavior.TakeLast };

        CollectionAssert.AreEqual(
            new[] { "StartArray", "StartObject", "Name:", "String:1", "Name:b", "String:2", "Name:", "String:3", "Name:a", "String:4", "EndObject", "EndArray" },
            Transcribe(Source, options));
        CollectionAssert.AreEqual(new[] { string.Empty, "b", string.Empty, "a" }, ReadHeaders(Source, options));
    }

    /// <summary>
    /// Verifies that a short record under the ragged policy exposes only its present fields.
    /// </summary>
    [TestMethod]
    public void Read_WhenRaggedShortRecord_ShouldExposeAvailableFields()
    {
        var options = new DelimitedReaderOptions { FieldCountBehavior = DelimitedFieldCountBehavior.Ragged };
        using DelimitedDocument document = DelimitedDocument.Parse("a,b,c\n1,2\n"u8, options);

        DelimitedElement record = document.RootElement[0];
        Assert.AreEqual("1", record.GetProperty("a").GetString());
        Assert.AreEqual("2", record.GetProperty("b").GetString());
        Assert.IsFalse(record.TryGetProperty("c", out _));
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedMalformedRecordBehavior.SkipRecord" /> skips a field-count-violating record and
    /// continues with the following valid record.
    /// </summary>
    [TestMethod]
    public void Read_WhenMalformedRecordSkip_ShouldRecoverAndContinue()
    {
        var options = new DelimitedReaderOptions { MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord };
        using DelimitedDocument document = DelimitedDocument.Parse("a,b\n1\n3,4\n"u8, options);

        Assert.AreEqual(1, document.RootElement.GetArrayLength());
        Assert.AreEqual("3", document.RootElement[0].GetProperty("a").GetString());
        Assert.AreEqual("4", document.RootElement[0].GetProperty("b").GetString());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedMalformedRecordBehavior.SkipRecord" /> skips a record with text after a
    /// closing quote whole, its quoted line break included, and goes on with the next line, rather than keeping the
    /// fields before the error or reading the text as a record.
    /// </summary>
    /// <param name="spansLines">Whether the malformed record's quoted field holds a line break.</param>
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public void Read_WhenMalformedRecordSkipAndTextFollowsAClosingQuote_ShouldSkipTheWholeRecord(bool spansLines)
    {
        var options = new DelimitedReaderOptions { NoHeader = true, MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord };
        string source = "x,y\n\"a" + (spansLines ? "\nb" : string.Empty) + "\"c,d\ne,f\n";

        List<string[]> records = ReadRecords(source, options);

        Assert.AreEqual(2, records.Count);
        CollectionAssert.AreEqual(new[] { "x", "y" }, records[0]);
        CollectionAssert.AreEqual(new[] { "e", "f" }, records[1]);
    }

    /// <summary>
    /// Verifies that in header mode <see cref="DelimitedMalformedRecordBehavior.SkipRecord" /> drops a record with text
    /// after a closing quote and binds the next record to the header.
    /// </summary>
    [TestMethod]
    public void Read_WhenMalformedRecordSkipAndTextFollowsAClosingQuoteInHeaderMode_ShouldBindTheNextRecord()
    {
        var options = new DelimitedReaderOptions { MalformedRecordBehavior = DelimitedMalformedRecordBehavior.SkipRecord };
        using DelimitedDocument document = DelimitedDocument.Parse("a,b\n1,\"2\"x\n3,4\n"u8, options);

        Assert.AreEqual(1, document.RootElement.GetArrayLength());
        Assert.AreEqual("3", document.RootElement[0].GetProperty("a").GetString());
        Assert.AreEqual("4", document.RootElement[0].GetProperty("b").GetString());
    }

    /// <summary>
    /// Reads the whole of a source and returns the header names the reader reports at its end.
    /// </summary>
    /// <param name="source">The delimited source text.</param>
    /// <param name="options">The reader options.</param>
    /// <returns>The header names.</returns>
    private static string[] ReadHeaders(string source, DelimitedReaderOptions options)
    {
        var reader = new Utf8DelimitedReader(Encoding.UTF8.GetBytes(source), options);
        while (reader.Read())
        {
        }

        return [.. reader.Headers];
    }
}
