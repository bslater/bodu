// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundBinaryFileTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the behavior of <see cref="CompoundBinaryFile" /> against a real-world compound file.
/// </summary>
[TestClass]
public partial class CompoundBinaryFileTests
{
    /// <summary>
    /// Opens the embedded sample compound file.
    /// </summary>
    /// <returns>An open <see cref="CompoundBinaryFile" /> over the sample fixture.</returns>
    private static CompoundBinaryFile OpenSample() =>
        CompoundBinaryFile.Open(CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound));

    /// <summary>
    /// Verifies that opening a valid compound file yields a readable instance exposing at least one directory entry.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Open_WhenStreamIsValidCompoundFile_ShouldReturnReadableInstance()
    {
        using CompoundBinaryFile file = OpenSample();

        Assert.IsTrue(file.Entries.Count > 0);
    }
}
