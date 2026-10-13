// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.TryGetSummaryInformation.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.PropertySets;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that reading the summary-information set of a disposed writable file throws
    /// <see cref="ObjectDisposedException" />, as the member documents, rather than reading the staging tree.
    /// </summary>
    [TestMethod]
    public void TryGetSummaryInformation_WhenWritableFileIsDisposed_ShouldThrowObjectDisposedException()
    {
        var file = CompoundFile.Create(new MemoryStream());
        file.SetSummaryInformation(new SummaryInformation(new SummaryInformationBuilder().ToPropertySet()));
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.TryGetSummaryInformation(out _);
        });
    }

    /// <summary>
    /// Verifies that reading the summary-information set of a disposed read-only file that holds none throws
    /// <see cref="ObjectDisposedException" />, as the member documents, rather than reporting the set as absent.
    /// </summary>
    [TestMethod]
    public void TryGetSummaryInformation_WhenReadOnlyFileWithoutTheSetIsDisposed_ShouldThrowObjectDisposedException()
    {
        var file = CompoundFile.Open(CompoundFixtures.OpenReference("valid/clean.dat"));
        file.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = file.TryGetSummaryInformation(out _);
        });
    }
}
