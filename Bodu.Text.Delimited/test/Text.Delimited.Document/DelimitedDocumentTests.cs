// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedDocumentTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited.Document;

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Contains tests for the read-only <see cref="DelimitedDocument" /> / <see cref="DelimitedElement" /> DOM.
/// </summary>
[TestClass]
public class DelimitedDocumentTests
{
    /// <summary>
    /// Verifies that the document root is an array whose records enumerate as header-keyed objects.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenHeaderMode_ShouldExposeRecordsAsObjects()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name,age\nAda,36\nGrace,45\n"u8);

        Assert.AreEqual(DelimitedValueKind.Array, document.RootElement.ValueKind);
        Assert.AreEqual(2, document.RootElement.GetArrayLength());

        DelimitedElement first = document.RootElement[0];
        Assert.AreEqual(DelimitedValueKind.Object, first.ValueKind);
        Assert.AreEqual("Ada", first.GetProperty("name").GetString());
        Assert.AreEqual("36", first.GetProperty("age").GetString());
    }

    /// <summary>
    /// Verifies that enumerating a record object yields its column/value properties in order.
    /// </summary>
    [TestMethod]
    public void EnumerateObject_WhenRecord_ShouldYieldColumnsInOrder()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("a,b\n1,2\n"u8);

        var pairs = new List<(string, string)>();
        foreach (DelimitedProperty property in document.RootElement[0].EnumerateObject())
            pairs.Add((property.Name, property.Value.GetString()));

        CollectionAssert.AreEqual(new List<(string, string)> { ("a", "1"), ("b", "2") }, pairs);
    }

    /// <summary>
    /// Verifies that headerless mode exposes records as positional arrays.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenHeaderless_ShouldExposeRecordsAsArrays()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("1,2\n3,4\n"u8, new Bodu.Text.Delimited.Reader.DelimitedReaderOptions { NoHeader = true });

        DelimitedElement record = document.RootElement[1];
        Assert.AreEqual(DelimitedValueKind.Array, record.ValueKind);
        Assert.AreEqual("3", record[0].GetString());
        Assert.AreEqual("4", record[1].GetString());
    }

    /// <summary>
    /// Verifies that accessing the root after disposal throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void RootElement_WhenDisposed_ShouldThrowObjectDisposedException()
    {
        DelimitedDocument document = DelimitedDocument.Parse("a\n1\n"u8);
        document.Dispose();

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = document.RootElement;
        });
    }
}
