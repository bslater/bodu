// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamTests.Seek.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Hashing;

public sealed partial class HashingStreamTests
{
    /// <summary>
    /// Verifies that <see cref="HashingStream.Seek" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Seek_WhenCalled_ShouldThrowNotSupportedException()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = stream.Seek(0, SeekOrigin.Begin);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.SetLength" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void SetLength_WhenCalled_ShouldThrowNotSupportedException()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.SetLength(10);
        });
    }

    /// <summary>
    /// Verifies that <see cref="HashingStream.Length" /> throws <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Length_WhenRead_ShouldThrowNotSupportedException()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = stream.Length;
        });
    }

    /// <summary>
    /// Verifies that both <see cref="HashingStream.Position" /> accessors throw
    /// <see cref="NotSupportedException" />.
    /// </summary>
    [TestMethod]
    public void Position_WhenReadOrWritten_ShouldThrowNotSupportedException()
    {
        using MemoryStream inner = new();
        using HashingStream stream = new(inner, new Fnv1a32());

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = stream.Position;
        });

        _ = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            stream.Position = 1;
        });
    }
}
