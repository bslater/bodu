// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Nodes;
using Bodu.Test;

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies that <see cref="CompoundFileBuilder" /> produces containers the reader can open and round-trip.
/// </summary>
[TestClass]
public partial class CompoundFileBuilderTests
{
    /// <summary>
    /// Verifies that a simple authored tree is serialized and read back with its structure and content intact.
    /// </summary>
    [TestMethod]
    [TestCategory(TestCategories.Smoke)]
    public void ToArray_WhenSimpleTree_ShouldRoundTripThroughReader()
    {
        var builder = new CompoundFileBuilder();
        _ = builder.Root.AddStream("Workbook", new byte[] { 0x09, 0x08, 0x10, 0x00 });
        CompoundStorageNode storage = builder.Root.AddStorage("Storage 1");
        _ = storage.AddStream("Nested", new byte[] { 1, 2, 3 });

        byte[] bytes = builder.ToArray();

        using CompoundFile file = CompoundFile.Open(new MemoryStream(bytes));
        Assert.IsTrue(file.RootStorage.TryOpenStream("Workbook", out CompoundStreamEntry? workbook));
        CollectionAssert.AreEqual(new byte[] { 0x09, 0x08, 0x10, 0x00 }, workbook.ReadAllBytes().ToArray());
        Assert.IsTrue(file.RootStorage.OpenStorage("Storage 1").TryOpenStream("Nested", out _));
    }

    /// <summary>
    /// Verifies that the root storage class identifier and entry metadata survive a write/read round-trip.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenRootHasClassId_ShouldPreserveClassId()
    {
        var clsid = new Guid("00020820-0000-0000-C000-000000000046");
        var builder = new CompoundFileBuilder();
        builder.Root.ClassId = clsid;
        _ = builder.Root.AddStream("Workbook", new byte[] { 1 });

        using CompoundFile file = CompoundFile.Open(new MemoryStream(builder.ToArray()));

        Assert.AreEqual(clsid, file.RootStorage.Stat.ClassId);
    }
}
