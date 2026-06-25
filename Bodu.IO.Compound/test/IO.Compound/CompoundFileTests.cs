// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies the behavior of <see cref="CompoundFile" /> against real-world and reference compound files.
/// </summary>
[TestClass]
public partial class CompoundFileTests
{
    /// <summary>
    /// Opens the embedded sample compound file.
    /// </summary>
    /// <returns>An open <see cref="CompoundFile" /> over the sample fixture.</returns>
    private static CompoundFile OpenSample() =>
        CompoundFile.Open(CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound));

    /// <summary>
    /// Verifies that opening a valid compound file yields a readable instance whose root storage exposes entries.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void Open_WhenStreamIsValidCompoundFile_ShouldReturnReadableInstance()
    {
        using CompoundFile file = OpenSample();

        Assert.IsNotEmpty(file.RootStorage.EnumerateEntries());
    }
}
