// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.ClearAndNullify.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ClearAndNullify{T}(ref T[])"/> zeroes the contents of the
    /// supplied unmanaged array and sets the caller's reference to <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ClearAndNullify_Array_WhenInvoked_ShouldZeroAndNullifyReference()
    {
        var array = new byte[] { 1, 2, 3, 4 };

        CryptoHelpers.ClearAndNullify(ref array);

        Assert.IsNull(array);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ClearAndNullify{T}(ref T[])"/> is a no-op when the supplied
    /// reference is <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void ClearAndNullify_Array_WhenNull_ShouldNotThrow()
    {
        byte[]? array = null;
        CryptoHelpers.ClearAndNullify(ref array);
        Assert.IsNull(array);
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ClearAndNullify(MemoryStream)"/> overwrites the underlying buffer,
    /// resets the stream length, and disposes the supplied <see cref="MemoryStream"/>.
    /// </summary>
    [TestMethod]
    public void ClearAndNullify_MemoryStream_WhenInvoked_ShouldClearAndDispose()
    {
        var stream = new MemoryStream();
        stream.WriteByte(0xAA);
        stream.WriteByte(0xBB);
        stream.WriteByte(0xCC);

        CryptoHelpers.ClearAndNullify(stream);

        Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = stream.Length;
        });
    }

    /// <summary>
    /// Verifies that <see cref="CryptoHelpers.ClearAndNullify(MemoryStream)"/> throws an
    /// <see cref="InvalidOperationException"/> when the supplied stream was constructed in a mode that suppresses
    /// buffer publication.
    /// </summary>
    [TestMethod]
    public void ClearAndNullify_MemoryStream_WhenBufferInaccessible_ShouldThrowExactly()
    {
        var bytes = new byte[] { 0x01, 0x02, 0x03 };
        var stream = new MemoryStream(bytes, index: 0, count: bytes.Length, writable: false, publiclyVisible: false);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            CryptoHelpers.ClearAndNullify(stream);
        });
    }
}
