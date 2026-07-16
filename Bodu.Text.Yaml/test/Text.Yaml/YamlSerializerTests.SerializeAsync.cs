// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.SerializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.IO;

namespace Bodu.Text.Yaml;

/// <summary>
/// Asynchronously serializes a value to a stream.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="YamlSerializer.SerializeAsync{T}(Stream, T, YamlSerializerOptions?,
    /// CancellationToken)" /> writes the same UTF-8 text to the stream as the in-memory overload returns.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenStreamDestination_ShouldWriteSameText()
    {
        var person = new Person { Name = "x", Age = 7, Active = true };
        using var destination = new MemoryStream();

        await YamlSerializer.SerializeAsync(destination, person);

        Assert.AreEqual(YamlSerializer.Serialize(person), Encoding.UTF8.GetString(destination.ToArray()));
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.SerializeAsync{T}(Stream, T, YamlSerializerOptions?,
    /// CancellationToken)" /> honors a token that is already canceled by faulting with
    /// <see cref="TaskCanceledException" /> before writing to the stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenTokenAlreadyCanceled_ShouldThrowTaskCanceledException()
    {
        var person = new Person { Name = "x", Age = 7, Active = true };
        using var destination = new MemoryStream();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await YamlSerializer.SerializeAsync(destination, person, options: null, cancellation.Token);
        });

        Assert.AreEqual(0, destination.Length);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.SerializeAsync{T}(Stream, T, YamlSerializerOptions?,
    /// CancellationToken)" /> throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>destination</c>
    /// when the stream is <see langword="null" />. The guard runs synchronously before the asynchronous write begins,
    /// so the exception also surfaces when the returned task is awaited.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenDestinationIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await YamlSerializer.SerializeAsync((Stream)null!, new Person { Name = "x" });
        });

        Assert.AreEqual("destination", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.SerializeAsync{T}(Stream, T, YamlSerializerOptions?,
    /// CancellationToken)" /> throws <see cref="ArgumentException" /> with <c>ParamName</c> <c>destination</c> when
    /// the stream does not support writing.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task SerializeAsync_WhenDestinationIsNotWritable_ShouldThrowArgumentException()
    {
        using var destination = new NonWritableStream();

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await YamlSerializer.SerializeAsync(destination, new Person { Name = "x" });
        });

        Assert.AreEqual("destination", ex.ParamName);
    }
}
