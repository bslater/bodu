// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Create.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using Bodu.IO.Compound.Builders;

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that a file created with the default build options is committed as version 3, with 512-byte sectors.
    /// </summary>
    [TestMethod]
    public void Create_WhenVersionIsDefault_ShouldWriteVersion3()
    {
        var destination = new MemoryStream();

        using (var file = CompoundFile.Create(destination, leaveOpen: true, options: default))
        {
            file.RootStorage.CreateStream("A", CompoundContainerBytes.Pattern(100));
            file.Commit();
        }

        byte[] committed = destination.ToArray();
        Assert.AreEqual(3, BinaryPrimitives.ReadUInt16LittleEndian(committed.AsSpan(0x1A)));
        Assert.AreEqual(9, BinaryPrimitives.ReadUInt16LittleEndian(committed.AsSpan(0x1E)));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Create(Stream, bool, CompoundBuildOptions)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> for a version that is neither the default nor a defined
    /// <see cref="CompoundFileVersion" />.
    /// </summary>
    [TestMethod]
    public void Create_WhenVersionIsUndefined_ShouldThrowArgumentOutOfRangeException()
    {
        var options = new CompoundBuildOptions { Version = (CompoundFileVersion)5 };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            using var file = CompoundFile.Create(new MemoryStream(), leaveOpen: false, options);
        });

        Assert.AreEqual("options", ex.ParamName);
    }
}
