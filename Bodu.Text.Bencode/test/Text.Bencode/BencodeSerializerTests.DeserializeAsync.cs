// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.DeserializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Text.Bencode;

/// <summary>
/// Asynchronously deserializes a value from a stream.
/// </summary>
public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that a value serialized to a stream with <see cref="BencodeSerializer.SerializeAsync{T}(Stream, T,
    /// BencodeSerializerOptions?, CancellationToken)" /> reads back equal through
    /// <see cref="BencodeSerializer.DeserializeAsync{T}(Stream, BencodeSerializerOptions?, CancellationToken)" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamRoundTrip_ShouldReturnEqualValue()
    {
        var original = new StreamModel { Id = 42, Label = "héllo" };
        using var stream = new MemoryStream();

        await BencodeSerializer.SerializeAsync(stream, original);
        stream.Position = 0;
        StreamModel roundTripped = await BencodeSerializer.DeserializeAsync<StreamModel>(stream);

        Assert.AreEqual(original.Id, roundTripped.Id);
        Assert.AreEqual(original.Label, roundTripped.Label);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.DeserializeAsync{T}(Stream, BencodeSerializerOptions?, CancellationToken)" />
    /// throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>source</c> when the stream is
    /// <see langword="null" />. The method body is asynchronous, so the guard's exception surfaces when the returned
    /// task is awaited.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await BencodeSerializer.DeserializeAsync<int>((Stream)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.DeserializeAsync{T}(Stream, BencodeSerializerOptions?, CancellationToken)" />
    /// throws <see cref="ArgumentException" /> with <c>ParamName</c> <c>source</c> when the stream does not support
    /// reading.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenSourceIsNotReadable_ShouldThrowArgumentException()
    {
        using var source = new NonReadableStream();

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await BencodeSerializer.DeserializeAsync<int>(source);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

}
