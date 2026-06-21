// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.RootStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that the root storage is exposed as a <see cref="CompoundEntryType.RootStorage" /> entry named
    /// <c>Root Entry</c>.
    /// </summary>
    [TestMethod]
    public void RootStorage_WhenSampleOpened_ShouldBeRootEntry()
    {
        using CompoundFile file = OpenSample();

        Assert.AreEqual("Root Entry", file.RootStorage.Name);
        Assert.AreEqual(CompoundEntryType.RootStorage, file.RootStorage.Stat.EntryType);
    }
}
