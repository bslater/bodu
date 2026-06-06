// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamExtensionsTests.ReadAllBytes.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Extensions;

public partial class StreamExtensionsTests
{
    /// <summary>
    /// Verifies that <c>ReadAllBytes</c> returns the full content of a seekable stream.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void ReadAllBytes_WhenStreamIsSeekable_ShouldReturnAllBytes()
    {
        byte[] content = [1, 2, 3, 4, 5];
        using MemoryStream stream = new(content);

        var result = stream.ReadAllBytes();

        CollectionAssert.AreEqual(content, result);
    }

    /// <summary>
    /// Verifies that <c>ReadAllBytes</c> reads only from the current position onward.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenPositionIsAdvanced_ShouldReturnRemainingBytes()
    {
        using MemoryStream stream = new([1, 2, 3, 4, 5]);
        stream.Position = 2;

        var result = stream.ReadAllBytes();

        CollectionAssert.AreEqual(new byte[] { 3, 4, 5 }, result);
    }

    /// <summary>
    /// Verifies that <c>ReadAllBytes</c> returns an empty array when no bytes remain.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenStreamIsAtEnd_ShouldReturnEmptyArray()
    {
        using MemoryStream stream = new([1, 2, 3]);
        stream.Position = stream.Length;

        var result = stream.ReadAllBytes();

        Assert.IsEmpty(result);
    }

    /// <summary>
    /// Verifies that <c>ReadAllBytes</c> reads the full content of a non-seekable stream.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenStreamIsNotSeekable_ShouldReturnAllBytes()
    {
        byte[] content = [10, 20, 30, 40];
        using NonSeekableMemoryStream stream = new(content);

        var result = stream.ReadAllBytes();

        CollectionAssert.AreEqual(content, result);
    }

    /// <summary>
    /// Verifies that <c>ReadAllBytes</c> throws <see cref="ArgumentNullException" /> when the stream is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void ReadAllBytes_WhenStreamIsNull_ShouldThrowExactly()
    {
        Stream stream = null!;

        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = stream.ReadAllBytes();
        });
    }
}
