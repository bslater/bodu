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

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Create(Stream, bool, CompoundBuildOptions)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> naming <c>options</c> for every version that is neither the default
    /// nor a defined <see cref="CompoundFileVersion" />.
    /// </summary>
    /// <param name="version">The undefined version.</param>
    [TestMethod]
    [DataRow(-1)]
    [DataRow(1)]
    [DataRow(2)]
    [DataRow(5)]
    [DataRow(int.MaxValue)]
    public void Create_WhenVersionIsAnyUndefinedValue_ShouldThrowArgumentOutOfRangeException(int version)
    {
        var options = new CompoundBuildOptions { Version = (CompoundFileVersion)version };

        ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
        {
            using var file = CompoundFile.Create(new MemoryStream(), leaveOpen: false, options);
        });

        Assert.AreEqual("options", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.Create(string, CompoundBuildOptions)" /> throws
    /// <see cref="ArgumentOutOfRangeException" /> naming <c>options</c> for an undefined version before it creates the
    /// file.
    /// </summary>
    [TestMethod]
    public void Create_WhenPathVersionIsUndefined_ShouldThrowBeforeCreatingTheFile()
    {
        string path = Path.Combine(Path.GetTempPath(), $"bodu-undefined-version-{Guid.NewGuid():N}.cfb");
        var options = new CompoundBuildOptions { Version = (CompoundFileVersion)5 };
        try
        {
            ArgumentOutOfRangeException ex = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                using var file = CompoundFile.Create(path, options);
            });

            Assert.AreEqual("options", ex.ParamName);
            Assert.IsFalse(File.Exists(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
