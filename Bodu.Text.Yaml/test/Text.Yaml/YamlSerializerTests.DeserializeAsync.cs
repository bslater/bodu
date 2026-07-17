// ---------------------------------------------------------------------------------------------------------------
// <copyright file="YamlSerializerTests.DeserializeAsync.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Test.IO;

namespace Bodu.Text.Yaml;

/// <summary>
/// Asynchronously deserializes a value from a stream.
/// </summary>
public partial class YamlSerializerTests
{
    /// <summary>
    /// Verifies that a value serialized to a stream with <see cref="YamlSerializer.SerializeAsync{T}(Stream, T,
    /// YamlSerializerOptions?, CancellationToken)" /> reads back equal through
    /// <see cref="YamlSerializer.DeserializeAsync{T}(Stream, YamlSerializerOptions?, CancellationToken)" />.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamRoundTrip_ShouldReturnEqualValue()
    {
        var original = new Person { Name = "héllo", Age = 42, Active = true };
        using var stream = new MemoryStream();

        await YamlSerializer.SerializeAsync(stream, original);
        stream.Position = 0;
        Person roundTripped = (await YamlSerializer.DeserializeAsync<Person>(stream))!;

        Assert.AreEqual(original.Name, roundTripped.Name);
        Assert.AreEqual(original.Age, roundTripped.Age);
        Assert.AreEqual(original.Active, roundTripped.Active);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.DeserializeAsync{T}(Stream, YamlSerializerOptions?,
    /// CancellationToken)" /> honors a token that is already canceled by faulting with
    /// <see cref="TaskCanceledException" /> before reading the stream.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenTokenAlreadyCanceled_ShouldThrowTaskCanceledException()
    {
        using var source = new MemoryStream(Encoding.UTF8.GetBytes("Name: x\nAge: 7\nActive: true\n"));
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        _ = await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            _ = await YamlSerializer.DeserializeAsync<Person>(source, options: null, cancellation.Token);
        });
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.DeserializeAsync{T}(Stream, YamlSerializerOptions?,
    /// CancellationToken)" /> reads a value from a stream that yields its bytes in small fixed-size chunks.
    /// </summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenStreamReadsInSmallChunks_ShouldReturnValue()
    {
        using var source = new FixedChunkStream(Encoding.UTF8.GetBytes("Name: x\nAge: 7\nActive: true\n"), chunkSize: 3);

        Person person = (await YamlSerializer.DeserializeAsync<Person>(source))!;

        Assert.AreEqual("x", person.Name);
        Assert.AreEqual(7, person.Age);
        Assert.IsTrue(person.Active);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.DeserializeAsync{T}(Stream, YamlSerializerOptions?,
    /// CancellationToken)" /> throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>source</c> when
    /// the stream is <see langword="null" />. The method body is asynchronous, so the guard's exception surfaces when
    /// the returned task is awaited.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenSourceIsNull_ShouldThrowArgumentNullException()
    {
        ArgumentNullException ex = await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            _ = await YamlSerializer.DeserializeAsync<Person>((Stream)null!);
        });

        Assert.AreEqual("source", ex.ParamName);
    }

    /// <summary>
    /// Verifies that <see cref="YamlSerializer.DeserializeAsync{T}(Stream, YamlSerializerOptions?,
    /// CancellationToken)" /> throws <see cref="ArgumentException" /> with <c>ParamName</c> <c>source</c> when the
    /// stream does not support reading.
    /// </summary>
    /// <returns>A task that completes when the assertion has run.</returns>
    [TestMethod]
    public async Task DeserializeAsync_WhenSourceIsNotReadable_ShouldThrowArgumentException()
    {
        using var source = new NonReadableStream();

        ArgumentException ex = await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            _ = await YamlSerializer.DeserializeAsync<Person>(source);
        });

        Assert.AreEqual("source", ex.ParamName);
    }
}
