// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.DeserializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bodu.Test.Assertions;
using Bodu.Test.IO;
using Bodu.Test.Kat;
using Bodu.Text.Toml.Document;
using Bodu.Text.Toml.Nodes;
using Bodu.Text.Toml.Reader;
using Bodu.Text.Toml.Serialization;
using Bodu.Text.Toml.Writer;

namespace Bodu.Text.Toml;

/// <summary>
/// Asynchronously deserializes a value from a stream.
/// </summary>
public partial class TomlSerializerTests
{
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
    /// Verifies that <see cref="TomlSerializer.DeserializeAsync{T}(Stream, TomlSerializerOptions?, CancellationToken)" />
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
            _ = await TomlSerializer.DeserializeAsync<GuardModel>((Stream)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.DeserializeAsync{T}(Stream, TomlSerializerOptions?, CancellationToken)" />
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
            _ = await TomlSerializer.DeserializeAsync<GuardModel>(source);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

}
