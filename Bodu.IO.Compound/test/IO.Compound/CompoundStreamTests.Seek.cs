// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundStreamTests.Seek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Compound;

/// <summary>
/// Verifies <see cref="CompoundStream.Seek(long, SeekOrigin)" /> on a read-only cursor.
/// </summary>
/// <remarks>
/// A writable cursor delegates straight to its in-memory buffer, so the origin arithmetic and the negative-target
/// rejection below belong to the read-only path and are only reachable there.
/// </remarks>
public partial class CompoundStreamTests
{
    /// <summary>
    /// Opens a read-only cursor over the shared multi-sector fixture.
    /// </summary>
    /// <param name="file">Receives the open compound file, which must outlive the cursor.</param>
    /// <param name="payload">Receives the stream payload.</param>
    /// <returns>The open cursor.</returns>
    private static CompoundStream OpenReadOnlyCursor(out CompoundFile file, out byte[] payload)
    {
        byte[] bytes = BuildContainer(out payload);
        file = CompoundFile.Open(new MemoryStream(bytes));
        return file.RootStorage.OpenStream("Data");
    }

    /// <summary>
    /// Verifies that <see cref="SeekOrigin.Current" /> moves the position relative to where the cursor already is,
    /// in both directions.
    /// </summary>
    [TestMethod]
    public void Seek_WhenOriginIsCurrent_ShouldMoveRelativeToPosition()
    {
        using CompoundStream stream = OpenReadOnlyCursor(out CompoundFile file, out _);
        using (file)
        {
            _ = stream.Seek(100, SeekOrigin.Begin);

            Assert.AreEqual(150L, stream.Seek(50, SeekOrigin.Current));
            Assert.AreEqual(120L, stream.Seek(-30, SeekOrigin.Current));
            Assert.AreEqual(120L, stream.Position);
        }
    }

    /// <summary>
    /// Verifies that <see cref="SeekOrigin.End" /> is measured from the payload length.
    /// </summary>
    [TestMethod]
    public void Seek_WhenOriginIsEnd_ShouldMoveRelativeToLength()
    {
        using CompoundStream stream = OpenReadOnlyCursor(out CompoundFile file, out byte[] payload);
        using (file)
        {
            Assert.AreEqual(payload.Length, stream.Seek(0, SeekOrigin.End));
            Assert.AreEqual(payload.Length - 10L, stream.Seek(-10, SeekOrigin.End));
        }
    }

    /// <summary>
    /// Verifies that seeking past the end is permitted - the <see cref="Stream" /> contract allows it - and that a
    /// read from there simply reports zero bytes.
    /// </summary>
    [TestMethod]
    public void Seek_WhenTargetIsBeyondEnd_ShouldSucceedAndReadZero()
    {
        using CompoundStream stream = OpenReadOnlyCursor(out CompoundFile file, out byte[] payload);
        using (file)
        {
            Assert.AreEqual(payload.Length + 500L, stream.Seek(payload.Length + 500, SeekOrigin.Begin));
            Assert.AreEqual(0, stream.Read(new byte[8]));
        }
    }

    /// <summary>
    /// Verifies that a seek resolving to a negative position throws <see cref="IOException" />, whichever origin
    /// produced it.
    /// </summary>
    /// <param name="offset">The seek offset.</param>
    /// <param name="origin">The seek origin.</param>
    [TestMethod]
    [DataRow(-1L, SeekOrigin.Begin)]
    [DataRow(-1L, SeekOrigin.Current)]
    [DataRow(-100000L, SeekOrigin.End)]
    public void Seek_WhenTargetIsNegative_ShouldThrowIOException(long offset, SeekOrigin origin)
    {
        using CompoundStream stream = OpenReadOnlyCursor(out CompoundFile file, out _);
        using (file)
        {
            _ = Assert.ThrowsExactly<IOException>(() =>
            {
                _ = stream.Seek(offset, origin);
            });
        }
    }

    /// <summary>
    /// Verifies that an origin outside the defined <see cref="SeekOrigin" /> values is rejected rather than treated
    /// as one of the valid ones.
    /// </summary>
    [TestMethod]
    public void Seek_WhenOriginIsNotDefined_ShouldThrowArgumentOutOfRangeException()
    {
        using CompoundStream stream = OpenReadOnlyCursor(out CompoundFile file, out _);
        using (file)
        {
            _ = Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = stream.Seek(0, (SeekOrigin)99);
            });
        }
    }
}
