// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.GetBufferOrThrowIfInaccessible.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.GetBufferOrThrowIfInaccessible(MemoryStream)"/> returns the underlying
    /// buffer segment when the <see cref="MemoryStream"/> was constructed in publicly visible mode.
    /// </summary>
    [TestMethod]
    public void GetBufferOrThrowIfInaccessible_WhenStreamExposesBuffer_ShouldReturnSegment()
    {
        using var stream = new MemoryStream();
        stream.WriteByte(0x01);
        stream.WriteByte(0x02);

        ArraySegment<byte> segment = CryptoHelpers.GetBufferOrThrowIfInaccessible(stream);

        Assert.IsNotNull(segment.Array);
        Assert.AreEqual(2, segment.Count);
        Assert.AreEqual(0x01, segment.Array[segment.Offset]);
        Assert.AreEqual(0x02, segment.Array[segment.Offset + 1]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.GetBufferOrThrowIfInaccessible(MemoryStream)"/> returns a segment
    /// with a zero count for an empty stream.
    /// </summary>
    [TestMethod]
    public void GetBufferOrThrowIfInaccessible_WhenStreamIsEmpty_ShouldReturnEmptySegment()
    {
        using var stream = new MemoryStream();

        ArraySegment<byte> segment = CryptoHelpers.GetBufferOrThrowIfInaccessible(stream);

        Assert.AreEqual(0, segment.Count);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.GetBufferOrThrowIfInaccessible(MemoryStream)"/> throws an
    /// <see cref="InvalidOperationException"/> when the <see cref="MemoryStream"/> was constructed in a mode
    /// that suppresses buffer publication.
    /// </summary>
    [TestMethod]
    public void GetBufferOrThrowIfInaccessible_WhenStreamHidesBuffer_ShouldThrowExactly()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03 };
        using var stream = new MemoryStream(bytes, index: 0, count: bytes.Length, writable: false, publiclyVisible: false);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            _ = CryptoHelpers.GetBufferOrThrowIfInaccessible(stream);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.GetBufferOrThrowIfInaccessible(MemoryStream)"/> throws an
    /// <see cref="ArgumentNullException"/> when the supplied stream is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void GetBufferOrThrowIfInaccessible_WhenStreamIsNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = CryptoHelpers.GetBufferOrThrowIfInaccessible(null!);
        });
    }
}
