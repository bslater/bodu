// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedElementTests.Indexer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Delimited.Reader;

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Contains the member backbone tests for the <see cref="DelimitedElement" /> indexer.
/// </summary>
public sealed partial class DelimitedElementTests
{
    /// <summary>
    /// Verifies that indexing the root array, or a positional record, after the owning document has been disposed throws
    /// <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void Indexer_WhenTheDocumentIsDisposed_ShouldThrowObjectDisposedException()
    {
        DelimitedDocument document = DelimitedDocument.Parse("a,b\n1,2\n"u8, new DelimitedReaderOptions { NoHeader = true });
        DelimitedElement root = document.RootElement;
        DelimitedElement record = root[0];
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root[0];
        });
        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = record[0];
        });
    }

    /// <summary>
    /// Verifies that an index outside the root array of two records throws <see cref="ArgumentOutOfRangeException" />
    /// naming the index.
    /// </summary>
    /// <param name="index">The out-of-range index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void Indexer_WhenTheIndexIsOutsideTheRootArray_ShouldThrowArgumentOutOfRangeException(int index)
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name\nAda\nGrace\n"u8);
        DelimitedElement root = document.RootElement;

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = root[index];
        });

        Assert.AreEqual("index", ex.ParamName);
    }

    /// <summary>
    /// Verifies that an index outside a positional record of two fields throws
    /// <see cref="ArgumentOutOfRangeException" /> naming the index.
    /// </summary>
    /// <param name="index">The out-of-range index.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(2)]
    public void Indexer_WhenTheIndexIsOutsideAPositionalRecord_ShouldThrowArgumentOutOfRangeException(int index)
    {
        using DelimitedDocument document = DelimitedDocument.Parse("a,b\n"u8, new DelimitedReaderOptions { NoHeader = true });
        DelimitedElement record = document.RootElement[0];

        var ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            _ = record[index];
        });

        Assert.AreEqual("index", ex.ParamName);
    }
}
