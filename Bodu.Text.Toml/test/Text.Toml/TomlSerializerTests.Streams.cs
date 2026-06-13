// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Streams.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.IO;

namespace Bodu.Text.Toml;

/// <summary>
/// Verifies the <see cref="Stream" />-based <see cref="TomlSerializer" /> overloads: asynchronous serialization writes
/// the same canonical UTF-8 text as the in-memory overload, and both the synchronous and asynchronous stream
/// deserialization overloads round-trip a value.
/// </summary>
public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="TomlSerializer.SerializeAsync{T}(Stream, T, TomlSerializerOptions?,
    /// CancellationToken)" /> writes the same canonical UTF-8 text to the stream as the in-memory overload returns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenStreamDestination_ShouldWriteCanonicalText()
    {
        var model = new StreamModel { Id = 7, Label = "x" };
        using var destination = new MemoryStream();

        await TomlSerializer.SerializeAsync(destination, model);

        Assert.AreEqual(TomlSerializer.Serialize(model), s_utf8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> reads a value from a
    /// stream positioned at a canonical document.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamSource_ShouldReturnValue()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("Id = 7\nLabel = \"x\"\n"));

        StreamModel model = TomlSerializer.Deserialize<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that a value serialized to a stream with <see cref="TomlSerializer.SerializeAsync{T}(Stream, T,
    /// TomlSerializerOptions?, CancellationToken)" /> reads back equal through
    /// <see cref="TomlSerializer.DeserializeAsync{T}(Stream, TomlSerializerOptions?, CancellationToken)" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamRoundTrip_ShouldReturnEqualValue()
    {
        var original = new StreamModel { Id = 42, Label = "héllo" };
        using var stream = new MemoryStream();

        await TomlSerializer.SerializeAsync(stream, original);
        stream.Position = 0;
        StreamModel roundTripped = await TomlSerializer.DeserializeAsync<StreamModel>(stream);

        Assert.AreEqual(original.Id, roundTripped.Id);
        Assert.AreEqual(original.Label, roundTripped.Label);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.SerializeAsync{T}(Stream, T, TomlSerializerOptions?,
    /// CancellationToken)" /> honors a token that is already canceled by faulting with
    /// <see cref="TaskCanceledException" /> before writing to the stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenTokenAlreadyCanceled_ShouldThrowTaskCanceledException()
    {
        var model = new StreamModel { Id = 7, Label = "x" };
        using var destination = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await TomlSerializer.SerializeAsync(destination, model, options: null, cancellation.Token);
        });

        Assert.AreEqual(0, destination.Length);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.DeserializeAsync{T}(Stream, TomlSerializerOptions?,
    /// CancellationToken)" /> honors a token that is already canceled by faulting with
    /// <see cref="TaskCanceledException" /> before reading the stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenTokenAlreadyCanceled_ShouldThrowTaskCanceledException()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("Id = 7\nLabel = \"x\"\n"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            _ = await TomlSerializer.DeserializeAsync<StreamModel>(source, options: null, cancellation.Token);
        });
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> reads a value from a
    /// stream that does not support seeking.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenStreamIsNonSeekable_ShouldReturnValue()
    {
        using var source = new NonSeekableStream(Encoding.UTF8.GetBytes("Id = 7\nLabel = \"x\"\n"));

        StreamModel model = TomlSerializer.Deserialize<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.DeserializeAsync{T}(Stream, TomlSerializerOptions?,
    /// CancellationToken)" /> reads a value from a stream that yields its bytes in small fixed-size chunks.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamReadsInSmallChunks_ShouldReturnValue()
    {
        using var source = new FixedChunkStream(Encoding.UTF8.GetBytes("Id = 7\nLabel = \"x\"\n"), chunkSize: 3);

        StreamModel model = await TomlSerializer.DeserializeAsync<StreamModel>(source);

        Assert.AreEqual(7, model.Id);
        Assert.AreEqual("x", model.Label);
    }

    /// <summary>
    /// A small model used by the stream overload tests.
    /// </summary>
    private sealed class StreamModel
    {
        /// <summary>Gets or sets the identifier.</summary>
        /// <returns>The identifier.</returns>
        public int Id { get; set; }

        /// <summary>Gets or sets the label.</summary>
        /// <returns>The label.</returns>
        public string Label { get; set; } = string.Empty;
    }
}
