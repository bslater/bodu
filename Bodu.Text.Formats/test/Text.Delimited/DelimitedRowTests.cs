// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedRowTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited;

/// <summary>
/// Unit tests for <see cref="DelimitedRow" />.
/// </summary>
[TestClass]
public sealed class DelimitedRowTests
{

    // ------------------------------------------------ Indexer[int] ----------------------------------------------

    /// <summary>
    /// Verifies that the integer indexer returns the correct field value for a valid index.
    /// </summary>
    [TestMethod]
    public void IndexerInt_WhenIndexIsValid_ShouldReturnField()
    {
        DelimitedDocument doc = Delimited.Parse("a,b,c\n1,2,3");

        Assert.AreEqual("2", doc.Rows[0][1]);
    }

    /// <summary>
    /// Verifies that the integer indexer throws <see cref="ArgumentOutOfRangeException" /> for a negative index.
    /// </summary>
    [TestMethod]
    public void IndexerInt_WhenIndexIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        DelimitedDocument doc = Delimited.Parse("a,b\n1,2");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = doc.Rows[0][-1];
        });
    }

    /// <summary>
    /// Verifies that the integer indexer throws <see cref="ArgumentOutOfRangeException" /> when the index is
    /// greater than or equal to the field count.
    /// </summary>
    [TestMethod]
    public void IndexerInt_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        DelimitedDocument doc = Delimited.Parse("a,b\n1,2");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = doc.Rows[0][5];
        });
    }

    // ----------------------------------------------- Indexer[string] -------------------------------------------

    /// <summary>
    /// Verifies that the string indexer returns the correct field value for a valid header name.
    /// </summary>
    [TestMethod]
    public void IndexerString_WhenHeaderExists_ShouldReturnField()
    {
        DelimitedDocument doc = Delimited.Parse("name,age\nAlice,30");

        Assert.AreEqual("Alice", doc.Rows[0]["name"]);
        Assert.AreEqual("30", doc.Rows[0]["age"]);
    }

    /// <summary>
    /// Verifies that the string indexer throws <see cref="InvalidOperationException" /> when the document was
    /// parsed without a header row.
    /// </summary>
    [TestMethod]
    public void IndexerString_WhenNoHeaderRow_ShouldThrowInvalidOperationException()
    {
        DelimitedParseOptions options = new() { HasHeader = false };
        DelimitedDocument doc = Delimited.Parse("Alice,30", options);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = doc.Rows[0]["name"];
        });
    }

    /// <summary>
    /// Verifies that the string indexer throws <see cref="ArgumentNullException" /> when the header argument is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void IndexerString_WhenHeaderIsNull_ShouldThrowArgumentNullException()
    {
        DelimitedDocument doc = Delimited.Parse("name\nAlice");

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = doc.Rows[0][null!];
        });
    }

    /// <summary>
    /// Verifies that the string indexer throws <see cref="KeyNotFoundException" /> when the header name is not
    /// found in the document.
    /// </summary>
    [TestMethod]
    public void IndexerString_WhenHeaderNotFound_ShouldThrowKeyNotFoundException()
    {
        DelimitedDocument doc = Delimited.Parse("name\nAlice");

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            _ = doc.Rows[0]["missing"];
        });
    }

    /// <summary>
    /// Verifies that the string indexer returns <see cref="string.Empty" /> when the header exists but the row
    /// has fewer fields than the header row.
    /// </summary>
    [TestMethod]
    public void IndexerString_WhenRowIsShorterThanHeader_ShouldReturnEmptyString()
    {
        DelimitedDocument doc = Delimited.Parse("a,b,c\n1,2");

        Assert.AreEqual(string.Empty, doc.Rows[0]["c"]);
    }

    // ----------------------------------------- GetValue<T> by index --------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.GetValue{T}(int)" /> parses the field as <see cref="int" /> for a
    /// valid index.
    /// </summary>
    [TestMethod]
    public void GetValueByIndex_WhenFieldIsValidInt_ShouldReturnParsedValue()
    {
        DelimitedDocument doc = Delimited.Parse("name,score\nAlice,42");

        var score = doc.Rows[0].GetValue<int>(1);

        Assert.AreEqual(42, score);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.GetValue{T}(int)" /> throws <see cref="ArgumentOutOfRangeException" />
    /// for an out-of-range index.
    /// </summary>
    [TestMethod]
    public void GetValueByIndex_WhenIndexIsOutOfRange_ShouldThrowArgumentOutOfRangeException()
    {
        DelimitedDocument doc = Delimited.Parse("a\n1");

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = doc.Rows[0].GetValue<int>(99);
        });
    }

    // ----------------------------------------- TryGetValue<T> by index -----------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.TryGetValue{T}(int, out T)" /> returns <see langword="true" /> and
    /// the parsed value for a valid index and parseable field.
    /// </summary>
    [TestMethod]
    public void TryGetValueByIndex_WhenValidIndexAndParseable_ShouldReturnTrueAndValue()
    {
        DelimitedDocument doc = Delimited.Parse("x,y\n10,20");

        var result = doc.Rows[0].TryGetValue<int>(0, out var x);

        Assert.IsTrue(result);
        Assert.AreEqual(10, x);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.TryGetValue{T}(int, out T)" /> returns <see langword="false" /> for
    /// an out-of-range index.
    /// </summary>
    [TestMethod]
    public void TryGetValueByIndex_WhenIndexIsOutOfRange_ShouldReturnFalse()
    {
        DelimitedDocument doc = Delimited.Parse("a\n1");

        var result = doc.Rows[0].TryGetValue<int>(99, out var _);

        Assert.IsFalse(result);
    }

    // ----------------------------------------- GetValue<T> by header -------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.GetValue{T}(string)" /> parses the named field as
    /// <see cref="int" />.
    /// </summary>
    [TestMethod]
    public void GetValueByHeader_WhenFieldIsValidInt_ShouldReturnParsedValue()
    {
        DelimitedDocument doc = Delimited.Parse("name,score\nAlice,99");

        var score = doc.Rows[0].GetValue<int>("score");

        Assert.AreEqual(99, score);
    }

    // --------------------------------------- TryGetValue<T> by header ------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.TryGetValue{T}(string, out T)" /> returns <see langword="false" />
    /// when the document has no headers.
    /// </summary>
    [TestMethod]
    public void TryGetValueByHeader_WhenNoHeaders_ShouldReturnFalse()
    {
        DelimitedParseOptions options = new() { HasHeader = false };
        DelimitedDocument doc = Delimited.Parse("1,2", options);

        var result = doc.Rows[0].TryGetValue<int>("missing", out var _);

        Assert.IsFalse(result);
    }

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.TryGetValue{T}(string, out T)" /> returns <see langword="true" />
    /// and the parsed value when the header exists and the field is parseable.
    /// </summary>
    [TestMethod]
    public void TryGetValueByHeader_WhenHeaderExistsAndFieldIsParseable_ShouldReturnTrueAndValue()
    {
        DelimitedDocument doc = Delimited.Parse("qty\n7");

        var result = doc.Rows[0].TryGetValue<int>("qty", out var qty);

        Assert.IsTrue(result);
        Assert.AreEqual(7, qty);
    }

    // ----------------------------------------------- Fields / Count -------------------------------------------

    /// <summary>
    /// Verifies that <see cref="DelimitedRow.Count" /> equals the number of fields in the row.
    /// </summary>
    [TestMethod]
    public void Count_ShouldEqualFieldCount()
    {
        DelimitedDocument doc = Delimited.Parse("a,b,c\n1,2,3");

        Assert.AreEqual(3, doc.Rows[0].Count);
    }

}
