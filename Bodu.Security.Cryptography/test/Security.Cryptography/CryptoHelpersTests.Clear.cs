// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CryptoHelpersTests.Clear.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class CryptoHelpersTests
{
    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Clear{T}(Memory{T})"/> zeroes the contents of the supplied
    /// memory buffer.
    /// </summary>
    [TestMethod]
    public void Clear_Memory_WhenInvoked_ShouldZeroAllBytes()
    {
        Memory<byte> memory = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };

        CryptographyHelper.Clear(memory);

        foreach (var b in memory.Span)
            Assert.AreEqual(0, b);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Clear{T}(Span{T})"/> zeroes the contents of the supplied span.
    /// </summary>
    [TestMethod]
    public void Clear_Span_WhenInvoked_ShouldZeroAllBytes()
    {
        var array = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        Span<byte> span = array;

        CryptographyHelper.Clear(span);

        foreach (var b in array)
            Assert.AreEqual(0, b);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Clear(byte[])"/> zeroes the contents of the supplied byte array.
    /// </summary>
    [TestMethod]
    public void Clear_ByteArray_WhenInvoked_ShouldZeroAllBytes()
    {
        var array = new byte[] { 1, 2, 3, 4 };

        CryptographyHelper.Clear(array);

        Assert.AreEqual(0, array[0]);
        Assert.AreEqual(0, array[1]);
        Assert.AreEqual(0, array[2]);
        Assert.AreEqual(0, array[3]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Clear(byte[])"/> is a no-op when the supplied array is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Clear_ByteArray_WhenNull_ShouldNotThrow() => CryptographyHelper.Clear((byte[]?)null);

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Clear{T}(T[])"/> zeroes the contents of the supplied unmanaged array.
    /// </summary>
    [TestMethod]
    public void Clear_GenericArray_WhenInvoked_ShouldZeroAllElements()
    {
        var array = new ulong[] { 0x1122334455667788UL, 0x99AABBCCDDEEFF00UL };

        CryptographyHelper.Clear(array);

        Assert.AreEqual(0UL, array[0]);
        Assert.AreEqual(0UL, array[1]);
    }

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Clear{T}(T[])"/> is a no-op when the supplied unmanaged array is
    /// <see langword="null"/>.
    /// </summary>
    [TestMethod]
    public void Clear_GenericArray_WhenNull_ShouldNotThrow() => CryptographyHelper.Clear<ulong>(null!);

    /// <summary>
    /// Verifies that <see cref="CryptographyHelper.Clear{T}(ref T)"/> zeroes the bytes of the supplied unmanaged value.
    /// </summary>
    [TestMethod]
    public void Clear_RefValue_WhenInvoked_ShouldZeroAllBytes()
    {
        var value = 0xDEADBEEFCAFEBABEUL;

        CryptographyHelper.Clear(ref value);

        Assert.AreEqual(0UL, value);
    }
}
