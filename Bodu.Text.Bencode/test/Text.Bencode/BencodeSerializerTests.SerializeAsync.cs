// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.SerializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Test.Kat;
using Bodu.Text.Bencode.Document;
using Bodu.Text.Bencode.Nodes;
using Bodu.Text.Bencode.Reader;
using Bodu.Text.Bencode.Serialization;
using Bodu.Text.Bencode.Writer;

namespace Bodu.Text.Bencode;

/// <summary>
/// Asynchronously serializes a value to a stream.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.SerializeAsync{T}(Stream, T, BencodeSerializerOptions?,
    /// CancellationToken)" /> writes the same canonical bytes to the stream as the in-memory overload returns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenStreamDestination_ShouldWriteCanonicalBytes()
    {
        var model = new StreamModel { Id = 7, Label = "x" };
        using var destination = new MemoryStream();

        await BencodeSerializer.SerializeAsync(destination, model);

        CollectionAssert.AreEqual(BencodeSerializer.Serialize(model), destination.ToArray());
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.SerializeAsync{T}(Stream, T, BencodeSerializerOptions?, CancellationToken)" />
    /// throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>destination</c> when the stream is
    /// <see langword="null" />. The guard runs synchronously before the asynchronous write begins, so the exception
    /// also surfaces when the returned task is awaited.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenDestinationIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await BencodeSerializer.SerializeAsync((Stream)null!, 5);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.SerializeAsync{T}(Stream, T, BencodeSerializerOptions?, CancellationToken)" />
    /// throws <see cref="ArgumentException" /> with <c>ParamName</c> <c>destination</c> when the stream does not
    /// support writing.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenDestinationIsNotWritable_ShouldThrowArgumentException()
    {
        using var destination = new NonWritableStream();

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await BencodeSerializer.SerializeAsync(destination, 5);
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

}
