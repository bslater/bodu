// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodeSerializerTests.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;
using Bodu.Test.IO;

namespace Bodu.Text.Bencode;

public partial class BencodeSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Serialize{T}(IBufferWriter{byte}, T, BencodeSerializerOptions?)" />
    /// throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>destination</c> when the buffer writer is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsNull_ForBufferWriterOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            BencodeSerializer.Serialize((IBufferWriter<byte>)null!, 5);
        }, "destination");
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

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(byte[], BencodeSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>data</c> when the array is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenDataIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeSerializer.Deserialize<int>((byte[])null!);
        }, "data");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(Stream, BencodeSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>source</c> when the stream is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNull_ForStreamOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = BencodeSerializer.Deserialize<int>((Stream)null!);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Deserialize{T}(Stream, BencodeSerializerOptions?)" /> throws
    /// <see cref="ArgumentException" /> with <c>ParamName</c> <c>source</c> when the stream does not support reading.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNotReadable_ShouldThrowArgumentException()
    {
        using var source = new NonReadableStream();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = BencodeSerializer.Deserialize<int>(source);
        }, "source");
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

    /// <summary>
    /// Verifies that <see cref="BencodeSerializer.Serialize{T}(IBufferWriter{byte}, T, BencodeSerializerOptions?)" />
    /// writes the value's canonical bytes to the buffer writer when the destination is valid, confirming the guard
    /// admits the supported case.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsBufferWriter_ShouldWriteCanonicalBytes()
    {
        var buffer = new ArrayBufferWriter<byte>();

        BencodeSerializer.Serialize(buffer, 5);

        Assert.AreEqual("i5e", Encoding.Latin1.GetString(buffer.WrittenSpan));
    }
}
