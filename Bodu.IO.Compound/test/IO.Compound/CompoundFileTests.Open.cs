// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that opening a <see langword="null" /> stream throws <see cref="ArgumentNullException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CompoundFile.Open(null!);
        });

        Assert.AreEqual("stream", ex.ParamName);
    }

    /// <summary>
    /// Verifies that opening data whose leading bytes are not the compound-file signature throws
    /// <see cref="CompoundFileFormatException" /> with the <see cref="CompoundFileError.InvalidSignature" /> category.
    /// </summary>
    [TestMethod]
    public void Open_WhenSignatureIsInvalid_ShouldThrowWithInvalidSignatureCategory()
    {
        using MemoryStream stream = new(new byte[600]);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(stream);
        });

        Assert.AreEqual(CompoundFileError.InvalidSignature, ex.Category);
    }

    /// <summary>
    /// Verifies that opening data shorter than a header throws <see cref="CompoundFileFormatException" /> with the
    /// <see cref="CompoundFileError.TruncatedFile" /> category.
    /// </summary>
    [TestMethod]
    public void Open_WhenDataIsTooShort_ShouldThrowWithTruncatedFileCategory()
    {
        using MemoryStream stream = new(new byte[64]);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(() =>
        {
            using var file = CompoundFile.Open(stream);
        });

        Assert.AreEqual(CompoundFileError.TruncatedFile, ex.Category);
    }

    /// <summary>
    /// Verifies that requesting an unsupported access mode throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Open_WhenModeIsNotRead_ShouldThrowNotSupportedException()
    {
        using MemoryStream stream = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound);

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            using var file = CompoundFile.Open(stream, (CompoundFileMode)0xFF);
        });
    }

    /// <summary>
    /// Verifies that the opened file reports the mode it was opened with.
    /// </summary>
    [TestMethod]
    public void Open_WhenModeIsRead_ShouldReportReadMode()
    {
        using CompoundFile file = OpenSample();

        Assert.AreEqual(CompoundFileMode.Read, file.Mode);
    }

    /// <summary>
    /// Verifies that disposing with <c>leaveOpen</c> set keeps the source stream usable afterwards.
    /// </summary>
    [TestMethod]
    public void Open_WhenLeaveOpenIsTrue_ShouldNotDisposeSourceStream()
    {
        using MemoryStream source = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound);

        using (var file = CompoundFile.Open(source, leaveOpen: true))
        {
            Assert.IsTrue(file.RootStorage.EnumerateEntries().Any());
        }

        Assert.IsTrue(source.CanRead);
    }
}
