// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.TryGetDocumentSummaryInformation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that reading the document-summary-information set of a disposed writable file throws
    /// <see cref="ObjectDisposedException" />, as the member documents, rather than reading the staging tree.
    /// </summary>
    [TestMethod]
    public void TryGetDocumentSummaryInformation_WhenWritableFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        var file = CompoundFile.Create(new MemoryStream());
        file.SetDocumentSummaryInformation(new DocumentSummaryInformation(new DocumentSummaryInformationBuilder().ToPropertySet()));
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.TryGetDocumentSummaryInformation(out _);
        });
    }

    /// <summary>
    /// Verifies that reading the document-summary-information set of a disposed read-only file that holds none throws
    /// <see cref="ObjectDisposedException" />, as the member documents, rather than reporting the set as absent.
    /// </summary>
    [TestMethod]
    public void TryGetDocumentSummaryInformation_WhenReadOnlyFileWithoutTheSetIsDisposed_ShouldThrowObjectDisposedException()
    {
        var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/clean.dat"));
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.TryGetDocumentSummaryInformation(out _);
        });
    }
}
