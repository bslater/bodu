// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Utf8DelimitedReaderTests.Policies.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

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
}
