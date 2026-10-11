// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageTests.Rename.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundStorageTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundStorage.Rename(string, string)" /> throws
    /// <see cref="KeyNotFoundException" /> when the storage has no child with the old name.
    /// </summary>
    [TestMethod]
    public void Rename_WhenOldNameIsMissing_ShouldThrowKeyNotFoundException()
    {
        using CompoundFile file = CreateWritableWithChild("A", existingIsStorage: false);

        Assert.ThrowsExactly<KeyNotFoundException>(() =>
        {
            file.RootStorage.Rename("Missing", "B");
        });

        AssertChildUnchanged(file, "A", isStorage: false);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.Rename(string, string)" /> throws <see cref="ArgumentException" /> when
    /// another child of the storage already has the new name, compared case-insensitively.
    /// </summary>
    /// <param name="newName">The new name, which another child already has.</param>
    [TestMethod]
    [DataRow("B")]
    [DataRow("b")]
    public void Rename_WhenNewNameIsTaken_ShouldThrowArgumentException(string newName)
    {
        using CompoundFile file = CreateWritableWithChild("A", existingIsStorage: false);
        _ = file.RootStorage.CreateStorage("B");

        ArgumentException ex = Assert.ThrowsExactly<ArgumentException>(() =>
        {
            file.RootStorage.Rename("A", newName);
        });

        Assert.AreEqual("newName", ex.ParamName);
        CollectionAssert.AreEquivalent(new[] { "A", "B" }, file.RootStorage.EnumerateEntries().Select(static e => e.Name).ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="CompoundStorage.Rename(string, string)" /> renames a child to a name that differs from
    /// its own only in case, which no other child has.
    /// </summary>
    [TestMethod]
    public void Rename_WhenNewNameDiffersOnlyInCase_ShouldRename()
    {
        using CompoundFile file = CreateWritableWithChild("Data", existingIsStorage: false);

        file.RootStorage.Rename("Data", "DATA");

        Assert.AreEqual("DATA", file.RootStorage.EnumerateEntries().Single().Name);
    }
}
