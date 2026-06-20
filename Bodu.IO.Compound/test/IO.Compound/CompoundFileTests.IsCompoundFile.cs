// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundFileTests.IsCompoundFile.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

public partial class CompoundFileTests
{
    /// <summary>
    /// Verifies that <see cref="CompoundFile.IsCompoundFile(Stream)" /> returns <see langword="true" /> for a compound
    /// file and restores the stream position.
    /// </summary>
    [TestMethod]
    public void IsCompoundFile_WhenStreamIsCompoundFile_ShouldReturnTrueAndRestorePosition()
    {
        using MemoryStream stream = CompoundFixtures.OpenStream(CompoundFixtures.SampleCompound);

        bool result = CompoundFile.IsCompoundFile(stream);

        Assert.IsTrue(result);
        Assert.AreEqual(0, stream.Position);

        // The check reads from the current position (consistent with Open) and restores it, so a stream positioned
        // past the signature reports false without losing its position.
        stream.Position = 5;
        Assert.IsFalse(CompoundFile.IsCompoundFile(stream));
        Assert.AreEqual(5, stream.Position);
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.IsCompoundFile(Stream)" /> returns <see langword="false" /> for data that
    /// is not a compound file.
    /// </summary>
    [TestMethod]
    public void IsCompoundFile_WhenStreamIsNotCompoundFile_ShouldReturnFalse()
    {
        using MemoryStream stream = new(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        Assert.IsFalse(CompoundFile.IsCompoundFile(stream));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.IsCompoundFile(Stream)" /> returns <see langword="false" /> for data that
    /// is shorter than the signature.
    /// </summary>
    [TestMethod]
    public void IsCompoundFile_WhenStreamShorterThanSignature_ShouldReturnFalse()
    {
        using MemoryStream stream = new(new byte[] { 0xD0, 0xCF, 0x11 });

        Assert.IsFalse(CompoundFile.IsCompoundFile(stream));
    }

    /// <summary>
    /// Verifies that <see cref="CompoundFile.IsCompoundFile(Stream)" /> throws <see cref="ArgumentException" /> for a
    /// non-seekable stream.
    /// </summary>
    [TestMethod]
    public void IsCompoundFile_WhenStreamIsNotSeekable_ShouldThrowArgumentException()
    {
        using Stream stream = new NonSeekableStream(CompoundFixtures.ReadBytes(CompoundFixtures.SampleCompound));

        _ = Assert.ThrowsExactly<ArgumentException>(() => CompoundFile.IsCompoundFile(stream));
    }

    /// <summary>
    /// Verifies that the span overload of <see cref="CompoundFile.IsCompoundFile(ReadOnlySpan{byte})" /> recognizes the
    /// signature.
    /// </summary>
    [TestMethod]
    public void IsCompoundFile_WhenSpanHasSignature_ShouldReturnTrue()
    {
        ReadOnlySpan<byte> data = [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1, 0x00];

        Assert.IsTrue(CompoundFile.IsCompoundFile(data));
    }

    /// <summary>
    /// A minimal non-seekable wrapper used to exercise the seekability guard.
    /// </summary>
    private sealed class NonSeekableStream : MemoryStream
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NonSeekableStream" /> class over the supplied bytes.
        /// </summary>
        /// <param name="buffer">The backing bytes.</param>
        public NonSeekableStream(byte[] buffer)
            : base(buffer)
        {
        }

        /// <inheritdoc />
        public override bool CanSeek => false;
    }
}
