// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.CreateStorage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStorage.CreateStorage(string)" /> throws <see cref="ArgumentException" /> for a
    /// name a child of the storage already has, compared case-insensitively, and leaves that child as it was.
    /// </summary>
    /// <param name="existing">The name the storage's child already has.</param>
    /// <param name="requested">The name passed to the call.</param>
    /// <param name="existingIsStorage">Whether the existing child is a storage rather than a stream.</param>
    [TestMethod]
    [DataRow("S", "S", true)]
    [DataRow("S", "s", true)]
    [DataRow("A", "A", false)]
    public void CreateStorage_WhenNameIsTaken_ShouldThrowArgumentException(string existing, string requested, bool existingIsStorage)
    {
        using CompoundFile file = CreateWritableWithChild(existing, existingIsStorage);

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            _ = file.RootStorage.CreateStorage(requested);
        });

        Assert.AreEqual("name", ex.ParamName);
        AssertChildUnchanged(file, existing, existingIsStorage);
    }
}
