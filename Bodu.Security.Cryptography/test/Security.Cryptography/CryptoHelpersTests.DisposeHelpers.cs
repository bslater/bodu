// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.DisposeHelpers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ClearAndNullify(MemoryStream)" /> zeroes the underlying buffer,
    /// resets the length to zero, and disposes the stream.
    /// </summary>
    [TestMethod]
    public void ClearAndNullify_MemoryStream_WhenExposesBuffer_ShouldClearAndDispose()
    {
        var stream = new MemoryStream();
        stream.WriteByte(0xAA);
        stream.WriteByte(0xBB);
        stream.WriteByte(0xCC);

        Assert.IsTrue(stream.TryGetBuffer(out ArraySegment<byte> originalSegment));
        var bufferReference = originalSegment.Array!;

        CryptoHelpers.ClearAndNullify(stream);

        for (var i = 0; i < bufferReference.Length; i++)
            Assert.AreEqual((byte)0, bufferReference[i], $"Buffer index {i} was not cleared.");

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = stream.Length;
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ClearAndNullify(MemoryStream)" /> throws
    /// <see cref="ArgumentNullException" /> when the stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ClearAndNullify_MemoryStream_WhenNull_ShouldThrowExactly()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            CryptoHelpers.ClearAndNullify((MemoryStream)null!);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ClearAndNullify(MemoryStream)" /> throws
    /// <see cref="InvalidOperationException" /> when the stream does not expose its underlying buffer
    /// (a non-publicly-visible <see cref="MemoryStream" />).
    /// </summary>
    [TestMethod]
    public void ClearAndNullify_MemoryStream_WhenBufferNotExposed_ShouldThrowExactly()
    {
        // MemoryStream constructed from a byte[] without the publiclyVisible flag does not expose
        // its underlying buffer through TryGetBuffer.
        var stream = new MemoryStream([1, 2, 3], writable: true);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            CryptoHelpers.ClearAndNullify(stream);
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.Clear{T}(T[])" /> is a no-op when the array reference is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Clear_Array_WhenNull_ShouldNotThrow()
    {
        int[]? array = null;
        CryptoHelpers.Clear(array!);
    }
}
