// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TomlSerializerTests.Guards.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers;
using System.Text;
using Bodu.Test.Assertions;

namespace Bodu.Text.Toml;

public partial class TomlSerializerTests
{
    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Serialize{T}(IBufferWriter{byte}, T, TomlSerializerOptions?)" />
    /// throws <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>destination</c> when the buffer writer is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsNull_ForBufferWriterOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            TomlSerializer.Serialize((IBufferWriter<byte>)null!, new GuardModel { Value = 5 });
        }, "destination");
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

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(string, TomlSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>text</c> when the text is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenTextIsNull_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlSerializer.Deserialize<GuardModel>((string)null!);
        }, "text");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> throws
    /// <see cref="ArgumentNullException" /> with <c>ParamName</c> <c>source</c> when the stream is
    /// <see langword="null" />.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNull_ForStreamOverload_ShouldThrowArgumentNullException()
    {
        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(() =>
        {
            _ = TomlSerializer.Deserialize<GuardModel>((Stream)null!);
        }, "source");
    }

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Deserialize{T}(Stream, TomlSerializerOptions?)" /> throws
    /// <see cref="ArgumentException" /> with <c>ParamName</c> <c>source</c> when the stream does not support reading.
    /// </summary>
    [TestMethod]
    public void Deserialize_WhenSourceIsNotReadable_ShouldThrowArgumentException()
    {
        using var source = new NonReadableStream();

        _ = ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(() =>
        {
            _ = TomlSerializer.Deserialize<GuardModel>(source);
        }, "source");
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

    /// <summary>
    /// Verifies that <see cref="TomlSerializer.Serialize{T}(IBufferWriter{byte}, T, TomlSerializerOptions?)" />
    /// writes the value's canonical text to the buffer writer when the destination is valid, confirming the guard
    /// admits the supported case.
    /// </summary>
    [TestMethod]
    public void Serialize_WhenDestinationIsBufferWriter_ShouldWriteCanonicalText()
    {
        var buffer = new ArrayBufferWriter<byte>();

        TomlSerializer.Serialize(buffer, new GuardModel { Value = 5 });

        Assert.AreEqual("Value = 5\n", Encoding.UTF8.GetString(buffer.WrittenSpan));
    }

    /// <summary>
    /// A minimal table-shaped model used by the guard tests so the document root satisfies the root-must-be-table
    /// rule once parameter validation passes.
    /// </summary>
    private sealed class GuardModel
    {
        /// <summary>
        /// Gets or sets the single integer member.
        /// </summary>
        /// <returns>The stored integer value.</returns>
        public int Value { get; set; }
    }

    /// <summary>
    /// A stream that reports <see cref="Stream.CanWrite" /> as <see langword="false" />, used to exercise the
    /// writability guard on stream-accepting entry points.
    /// </summary>
    private sealed class NonWritableStream : MemoryStream
    {
        /// <summary>
        /// Gets a value indicating whether the stream supports writing.
        /// </summary>
        /// <returns>Always <see langword="false" />.</returns>
        public override bool CanWrite => false;
    }

    /// <summary>
    /// A stream that reports <see cref="Stream.CanRead" /> as <see langword="false" />, used to exercise the
    /// readability guard on stream-accepting entry points.
    /// </summary>
    private sealed class NonReadableStream : MemoryStream
    {
        /// <summary>
        /// Gets a value indicating whether the stream supports reading.
        /// </summary>
        /// <returns>Always <see langword="false" />.</returns>
        public override bool CanRead => false;
    }
}
