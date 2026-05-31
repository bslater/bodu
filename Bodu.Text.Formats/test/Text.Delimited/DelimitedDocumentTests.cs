// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedDocumentTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Unit tests for <see cref="DelimitedDocument" />.
/// </summary>
[TestClass]
public sealed class DelimitedDocumentTests
{

    // ------------------------------------------------- Headers --------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.Headers" /> is empty when the source is empty.
    /// </summary>
    [TestMethod]
    public void Headers_WhenSourceIsEmpty_ShouldBeEmpty()
    {
        DelimitedDocument doc = Delimited.Parse(string.Empty);

        Assert.AreEqual(0, doc.Headers.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.Headers" /> is populated from the first row when
    /// <see cref="DelimitedParseOptions.HasHeader" /> is <see langword="true" /> (the default).
    /// </summary>
    [TestMethod]
    public void Headers_WhenHasHeaderEnabled_ShouldExposeFirstRowAsHeaders()
    {
        DelimitedDocument doc = Delimited.Parse("name,age,city\nAlice,30,Paris");

        CollectionAssert.AreEqual(new[] { "name", "age", "city" }, doc.Headers.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.Headers" /> is empty when
    /// <see cref="DelimitedParseOptions.HasHeader" /> is <see langword="false" />.
    /// </summary>
    [TestMethod]
    public void Headers_WhenHasHeaderDisabled_ShouldBeEmpty()
    {
        DelimitedParseOptions options = new() { HasHeader = false };

        DelimitedDocument doc = Delimited.Parse("Alice,30,Paris", options);

        Assert.AreEqual(0, doc.Headers.Count);
    }

    // -------------------------------------------------- Rows ----------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.Rows" /> is empty for an empty source.
    /// </summary>
    [TestMethod]
    public void Rows_WhenSourceIsEmpty_ShouldBeEmpty()
    {
        DelimitedDocument doc = Delimited.Parse(string.Empty);

        Assert.AreEqual(0, doc.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.Rows" /> does not include the header row.
    /// </summary>
    [TestMethod]
    public void Rows_WhenHasHeaderEnabled_ShouldNotIncludeHeaderRow()
    {
        DelimitedDocument doc = Delimited.Parse("name,age\nAlice,30\nBob,25");

        Assert.AreEqual(2, doc.Rows.Count);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.Rows" /> preserves source order.
    /// </summary>
    [TestMethod]
    public void Rows_ShouldPreserveSourceOrder()
    {
        DelimitedDocument doc = Delimited.Parse("name\nAlice\nBob\nCarol");

        var names = doc.Rows.Select(r => r[0]).ToArray();

        CollectionAssert.AreEqual(new[] { "Alice", "Bob", "Carol" }, names);
    }

    // ----------------------------------------------- FieldCount -------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.FieldCount" /> equals the header column count when a header row
    /// is present.
    /// </summary>
    [TestMethod]
    public void FieldCount_WhenHasHeaderEnabled_ShouldEqualHeaderColumnCount()
    {
        DelimitedDocument doc = Delimited.Parse("a,b,c\n1,2,3");

        Assert.AreEqual(3, doc.FieldCount);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.FieldCount" /> equals the field count of the first data row
    /// when no header is present.
    /// </summary>
    [TestMethod]
    public void FieldCount_WhenHasHeaderDisabled_ShouldEqualFirstRowFieldCount()
    {
        DelimitedParseOptions options = new() { HasHeader = false };

        DelimitedDocument doc = Delimited.Parse("1,2,3,4", options);

        Assert.AreEqual(4, doc.FieldCount);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedDocument.FieldCount" /> is <c>0</c> when the document is empty.
    /// </summary>
    [TestMethod]
    public void FieldCount_WhenDocumentIsEmpty_ShouldBeZero()
    {
        DelimitedDocument doc = Delimited.Parse(string.Empty);

        Assert.AreEqual(0, doc.FieldCount);
    }

}
