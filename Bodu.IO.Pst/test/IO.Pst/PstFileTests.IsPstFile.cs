// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.IsPstFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Test.Kat;

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Gets one manifest row per corpus fixture, every variant included — the sniff recognizes all of them.
    /// </summary>
    /// <value>One row per fixture.</value>
    public static IEnumerable<object[]> AllFixtureRows =>
        PstReferenceFixtures.Manifest.Fixtures.Select(f => new object[] { f });

    /// <summary>
    /// Verifies that the sniff recognizes every corpus fixture, including the ANSI variants the reader cannot open.
    /// </summary>
    /// <param name="fixture">The manifest row of the fixture.</param>
    [TestMethod]
    [DynamicData(
        nameof(AllFixtureRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void IsPstFile_WhenReferenceFixture_ShouldReturnTrue(PstReferenceFixture fixture)
    {
        using MemoryStream stream = PstReferenceFixtures.OpenStream(fixture.File);

        Assert.IsTrue(PstFile.IsPstFile(stream));
    }

    /// <summary>
    /// Verifies that the sniff rejects data that does not carry the PST magics, and short streams.
    /// </summary>
    [TestMethod]
    public void IsPstFile_WhenNotPstData_ShouldReturnFalse()
    {
        using var zeros = new MemoryStream(new byte[64]);
        Assert.IsFalse(PstFile.IsPstFile(zeros));

        using var empty = new MemoryStream();
        Assert.IsFalse(PstFile.IsPstFile(empty));
    }

    /// <summary>
    /// Verifies that the sniff restores the stream position it advanced.
    /// </summary>
    [TestMethod]
    public void IsPstFile_WhenCalled_ShouldRestorePosition()
    {
        using MemoryStream stream = PstReferenceFixtures.OpenStream(Sample1);
        stream.Position = 3;

        _ = PstFile.IsPstFile(stream);

        Assert.AreEqual(3, stream.Position);
    }

    /// <summary>
    /// Verifies that a non-seekable stream reports <see langword="false" /> rather than throwing.
    /// </summary>
    [TestMethod]
    public void IsPstFile_WhenStreamNotSeekable_ShouldReturnFalse()
    {
        byte[] bytes;
        using (MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1))
            bytes = fixture.ToArray();

        using var stream = new NonSeekableStream(bytes);

        Assert.IsFalse(PstFile.IsPstFile(stream));
    }

    /// <summary>
    /// Verifies the argument guard.
    /// </summary>
    [TestMethod]
    public void IsPstFile_WhenStreamNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = PstFile.IsPstFile(null!);
            },
            "stream");
    }
}
