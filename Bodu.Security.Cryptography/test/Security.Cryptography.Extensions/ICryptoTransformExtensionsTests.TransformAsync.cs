// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests.TransformAsync.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;
using Bodu.Test.IO;

namespace Bodu.Security.Cryptography.Extensions;

public partial class ICryptoTransformExtensionsTests
{
    // ---------------------------------------------------------------------------------------------------------------
    // TransformAsync(Stream, Stream, int, CancellationToken)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="transform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;
        using var source = new MemoryStream([1, 2, 3, 4]);
        using var target = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await transform!.TransformAsync(source, target, 16);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="sourceStream" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenSourceStreamIsNull_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var target = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await transform.TransformAsync(null!, target, 16);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="targetStream" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenTargetStreamIsNull_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream([1, 2, 3, 4]);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await transform.TransformAsync(source, null!, 16);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is zero.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenBufferSizeIsZero_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream([1, 2, 3, 4]);
        using var target = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
        {
            await transform.TransformAsync(source, target, 0);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="ArgumentOutOfRangeException" /> when <paramref name="bufferSize" /> is negative.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenBufferSizeIsNegative_ShouldThrowExactly()
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream([1, 2, 3, 4]);
        using var target = new MemoryStream();

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(async () =>
        {
            await transform.TransformAsync(source, target, -1);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// reads all bytes from the source, applies the transform, and writes the expected output to the target stream.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Stream_WhenInputIsValid_ShouldWriteTransformedBytesToTarget(KnownAnswerTest kat)
    {
        using var source = new MemoryStream(kat.Input);
        using var target = new MemoryStream();
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);

        await transform.TransformAsync(source, target, bufferSize: kat.Input.Length);

        CollectionAssert.AreEqual(kat.ExpectedOutput, target.ToArray(),
            $"[{kat.Name}] Target stream content does not match expected output.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// writes nothing to the target stream when the source stream is empty.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenSourceIsEmpty_ShouldWriteNothingToTarget()
    {
        using var source = new MemoryStream(Array.Empty<byte>());
        using var target = new MemoryStream();
        using SimpleReversingCryptoTransform transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

        await transform.TransformAsync(source, target, bufferSize: 16);

        Assert.AreEqual(0, target.Length,
            "Target stream must remain empty when the source stream contains no data.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// does not dispose the target stream after completing the operation.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Stream_WhenCompleted_ShouldLeaveTargetStreamOpen(KnownAnswerTest kat)
    {
        using var source = new MemoryStream(kat.Input);
        var target = new MemoryStream();
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);

        await transform.TransformAsync(source, target, bufferSize: kat.Input.Length);

        Assert.IsTrue(target.CanWrite,
            $"[{kat.Name}] Target stream should remain open after TransformAsync.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="TaskCanceledException" /> when the cancellation token is signalled while
    /// <c>ReadAsync</c> is blocked. The implementation catches the <see cref="OperationCanceledException" />
    /// raised by the stream and converts it to a fresh <see cref="TaskCanceledException" />, so the exact
    /// subtype is contractually significant.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenCancelledDuringRead_ShouldThrowExactly()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        using var source = new ThrottledIncrementingByteStream(512, readDelay: 1000);
        using var target = new MemoryStream();
        using ICryptoTransform encryptor = algorithm.CreateEncryptor();
        using var cts = new CancellationTokenSource(millisecondsDelay: 150);

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await encryptor.TransformAsync(source, target, bufferSize: 32, cts.Token);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="TaskCanceledException" /> when the cancellation token is already cancelled before
    /// the call. The cancelled <c>ReadAsync</c> raises <see cref="OperationCanceledException" />, which the
    /// implementation converts to a fresh <see cref="TaskCanceledException" />.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Stream_WhenAlreadyCancelled_ShouldThrowExactly(KnownAnswerTest kat)
    {
        using var source = new MemoryStream(kat.Input);
        using var target = new MemoryStream();
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await transform.TransformAsync(source, target, bufferSize: kat.Input.Length, cts.Token);
        });
    }

    /// <summary>
    /// Verifies that a round-trip using <see cref="ICryptoTransformExtensions.TransformAsync" /> for
    /// encryption followed by <see cref="ICryptoTransformExtensions.TransformAsync" /> for decryption
    /// produces the original plaintext.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_RoundTrip_ShouldProduceOriginalPlaintext()
    {
        using SymmetricAlgorithm algorithm = CreateAlgorithm();
        var plainText = System.Text.Encoding.UTF8.GetBytes("round-trip-test");

        // Encrypt
        using var sourceStream = new MemoryStream(plainText);
        using var encryptedStream = new MemoryStream();
        using (ICryptoTransform encryptor = algorithm.CreateEncryptor())
            await encryptor.TransformAsync(sourceStream, encryptedStream, bufferSize: 32);

        // Decrypt
        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();
        using (ICryptoTransform decryptor = algorithm.CreateDecryptor())
            await decryptor.TransformAsync(encryptedStream, decryptedStream, bufferSize: 32);

        CollectionAssert.AreEqual(plainText, decryptedStream.ToArray(),
            "Round-trip encrypt+decrypt must recover the original plaintext.");
    }

    // ---------------------------------------------------------------------------------------------------------------
    // TransformAsync(ReadOnlyMemory<byte>, Memory<byte>, CancellationToken)
    // ---------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
    /// throws <see cref="ArgumentNullException" /> when <paramref name="transform" /> is <see langword="null" />.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Memory_WhenTransformIsNull_ShouldThrowExactly()
    {
        ICryptoTransform? transform = null;
        var input = new ReadOnlyMemory<byte>([1, 2, 3, 4]);
        var destination = new Memory<byte>(new byte[64]);

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await transform!.TransformAsync(input, destination);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
    /// transforms the input memory region and writes the result into the destination, returning the
    /// correct byte count and producing the expected output.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Memory_WhenInputIsValid_ShouldWriteTransformedBytesToDestination(
        KnownAnswerTest kat)
    {
        var dest = new byte[kat.Input.Length + 16];
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);

        var written = await transform.TransformAsync(
            new ReadOnlyMemory<byte>(kat.Input),
            new Memory<byte>(dest));

        Assert.AreEqual(kat.ExpectedOutput.Length, written,
            $"[{kat.Name}] Written byte count does not match expected output length.");
        CollectionAssert.AreEqual(kat.ExpectedOutput, dest[..written],
            $"[{kat.Name}] Transformed output does not match expected.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
    /// produces output identical to the synchronous <c>Transform(ReadOnlySpan, Span)</c> overload
    /// for the same input.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Memory_WhenComparedToSyncOverload_ShouldProduceIdenticalOutput(
        KnownAnswerTest kat)
    {
        var syncDest = new byte[kat.Input.Length + 16];
        int syncWritten;
        using (SimpleReversingCryptoTransform syncTransform = CreateTransform(kat))
            syncWritten = syncTransform.Transform(kat.Input.AsSpan(), syncDest.AsSpan());

        var asyncDest = new byte[kat.Input.Length + 16];
        int asyncWritten;
        using (SimpleReversingCryptoTransform asyncTransform = CreateTransform(kat))
            asyncWritten = await asyncTransform.TransformAsync(
                new ReadOnlyMemory<byte>(kat.Input),
                new Memory<byte>(asyncDest));

        Assert.AreEqual(syncWritten, asyncWritten,
            $"[{kat.Name}] Byte counts differ between sync and async overloads.");
        CollectionAssert.AreEqual(
            syncDest[..syncWritten],
            asyncDest[..asyncWritten],
            $"[{kat.Name}] Sync and async overloads produced different output.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
    /// throws <see cref="TaskCanceledException" /> when the token is already cancelled before the operation begins.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Memory_WhenAlreadyCancelled_ShouldThrowExactly(
        KnownAnswerTest kat)
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);
        var destination = new Memory<byte>(new byte[kat.Input.Length + 16]);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await transform.TransformAsync(
                new ReadOnlyMemory<byte>(kat.Input),
                destination,
                cts.Token);
        });
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,ReadOnlyMemory{byte},Memory{byte},CancellationToken)" />
    /// throws <see cref="ArgumentException" /> when the destination buffer is too small to hold the output.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Memory_WhenDestinationIsTooSmall_ShouldThrowExactly(
        KnownAnswerTest kat)
    {
        using SimpleReversingCryptoTransform transform = CreateTransform(kat);

        // A 1-byte destination is always too small for any block-sized input.
        var tooSmall = new Memory<byte>(new byte[1]);

        await Assert.ThrowsExactlyAsync<ArgumentException>(async () =>
        {
            await transform.TransformAsync(
                new ReadOnlyMemory<byte>(kat.Input),
                tooSmall);
        });
    }
}
