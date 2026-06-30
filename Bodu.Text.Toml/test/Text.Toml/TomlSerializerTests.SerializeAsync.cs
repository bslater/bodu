// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.SerializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;

namespace Bodu.Text.Toml;

/// <summary>
/// Asynchronously serializes a value to a stream.
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
    /// Verifies that <see cref="TomlSerializer.SerializeAsync{T}(Stream, T, TomlSerializerOptions?, CancellationToken)" />
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
            await TomlSerializer.SerializeAsync((Stream)null!, new GuardModel { Value = 5 });
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.SerializeAsync{T}(Stream, T, TomlSerializerOptions?, CancellationToken)" />
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
            await TomlSerializer.SerializeAsync(destination, new GuardModel { Value = 5 });
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

}
