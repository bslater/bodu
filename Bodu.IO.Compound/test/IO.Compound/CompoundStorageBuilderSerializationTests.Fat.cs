// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStorageBuilderSerializationTests.Fat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;
using Bodu.IO.Compound.Internal;

namespace Bodu.IO.Compound;

public partial class CompoundStorageBuilderSerializationTests
{
    /// <summary>
    /// Verifies that every FAT entry for a sector past the end of the file is written FREESECT, as [MS-CFB] marks an
    /// unallocated sector.
    /// </summary>
    [TestMethod]
    public void ToArray_WhenFatHasUnusedEntries_ShouldMarkThemFree()
    {
        var builder = CompoundStorageBuilder.CreateRoot();
        _ = builder.AddStream("A", CompoundContainerBytes.Pattern(100));

        byte[] bytes = builder.ToArray();

        uint[] fat = CompoundContainerBytes.ReadFat(bytes);
        int sectors = (bytes.Length / 512) - 1;
        Assert.IsLessThan(fat.Length, sectors);
        CollectionAssert.DoesNotContain(fat[..sectors], CfbHeader.FreeSector);
        foreach (uint entry in fat[sectors..])
            Assert.AreEqual(CfbHeader.FreeSector, entry);
    }
}
