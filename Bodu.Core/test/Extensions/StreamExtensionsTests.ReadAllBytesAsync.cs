// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensionsTests.ReadAllBytesAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StreamExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ReadAllBytesAsync</c> returns the full content of a seekable stream.
    /// </summary>
    [TestMethod]
    public async Task ReadAllBytesAsync_WhenStreamIsSeekable_ShouldReturnAllBytes()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using MemoryStream stream = new(content);

        byte[] result = await stream.ReadAllBytesAsync();

        CollectionAssert.AreEqual(content, result);
    }

    /// <summary>
    /// Verifies that <c>ReadAllBytesAsync</c> reads the full content of a non-seekable stream.
    /// </summary>
    [TestMethod]
    public async Task ReadAllBytesAsync_WhenStreamIsNotSeekable_ShouldReturnAllBytes()
    {
        byte[] content = [9, 8, 7, 6];
        using NonSeekableMemoryStream stream = new(content);

        byte[] result = await stream.ReadAllBytesAsync();

        CollectionAssert.AreEqual(content, result);
    }

    /// <summary>
    /// Verifies that <c>ReadAllBytesAsync</c> returns an empty array when no bytes remain.
    /// </summary>
    [TestMethod]
    public async Task ReadAllBytesAsync_WhenStreamIsAtEnd_ShouldReturnEmptyArray()
    {
        using MemoryStream stream = new([1, 2, 3]);
        stream.Position = stream.Length;

        byte[] result = await stream.ReadAllBytesAsync();

        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that <c>ReadAllBytesAsync</c> throws <see cref="ArgumentNullException" /> synchronously when the
    /// stream is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ReadAllBytesAsync_WhenStreamIsNull_ShouldThrowExactly()
    {
        Stream stream = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = stream.ReadAllBytesAsync();
        });
    }
}
