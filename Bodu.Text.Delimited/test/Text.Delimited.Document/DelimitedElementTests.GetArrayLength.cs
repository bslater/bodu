// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelimitedElementTests.GetArrayLength.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Delimited.Document;

/// <summary>
/// Contains the member backbone tests for <see cref="DelimitedElement.GetArrayLength" />.
/// </summary>
public sealed partial class DelimitedElementTests
{
    /// <summary>
    /// Verifies that the root array's length is the number of records.
    /// </summary>
    [TestMethod]
    public void GetArrayLength_WhenRootIsArray_ShouldReturnTheRecordCount()
    {
        using DelimitedDocument document = DelimitedDocument.Parse("name\nAda\nGrace\n"u8);

        Assert.AreEqual(2, document.RootElement.GetArrayLength());
    }

    /// <summary>
    /// Verifies that reading the root array's length after the owning document has been disposed throws
    /// <see cref="ObjectDisposedException" /> rather than reporting an empty document.
    /// </summary>
    [TestMethod]
    public void GetArrayLength_WhenTheDocumentIsDisposed_ShouldThrowObjectDisposedException()
    {
        DelimitedDocument document = DelimitedDocument.Parse("name\nAda\nGrace\n"u8);
        DelimitedElement root = document.RootElement;
        document.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = root.GetArrayLength();
        });
    }
}
