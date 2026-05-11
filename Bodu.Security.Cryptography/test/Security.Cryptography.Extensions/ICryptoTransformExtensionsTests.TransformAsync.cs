// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ICryptoTransformExtensionsTests.TransformAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.IO;
using System.Security.Cryptography;

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
    public async Task TransformAsync_Stream_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        ICryptoTransform? transform = null;
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
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
    public async Task TransformAsync_Stream_WhenSourceStreamIsNull_ShouldThrowArgumentNullException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
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
    public async Task TransformAsync_Stream_WhenTargetStreamIsNull_ShouldThrowArgumentNullException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });

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
    public async Task TransformAsync_Stream_WhenBufferSizeIsZero_ShouldThrowArgumentOutOfRangeException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
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
    public async Task TransformAsync_Stream_WhenBufferSizeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);
        using var source = new MemoryStream(new byte[] { 1, 2, 3, 4 });
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
        using var transform = CreateTransform(kat);

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
        using var transform = CreateTransform(GetValidTransformTestData().First()[0] as KnownAnswerTest);

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
        using var transform = CreateTransform(kat);

        await transform.TransformAsync(source, target, bufferSize: kat.Input.Length);

        Assert.IsTrue(target.CanWrite,
            $"[{kat.Name}] Target stream should remain open after TransformAsync.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws <see cref="OperationCanceledException" /> or <see cref="TaskCanceledException" /> when the
    /// cancellation token is signalled during a slow read operation.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenCancelled_ShouldThrowCancelledException()
    {
        using var algorithm = CreateAlgorithm();
        algorithm.Padding = PaddingMode.None;

        using var source = new ThrottledIncrementingByteStream(512, readDelay: 1000);
        using var target = new MemoryStream();
        using var encryptor = algorithm.CreateEncryptor();
        using var cts = new CancellationTokenSource(millisecondsDelay: 150);

        try
        {
            await encryptor.TransformAsync(source, target, bufferSize: 32, cts.Token);
            Assert.Fail("Expected OperationCanceledException or TaskCanceledException.");
        }
        catch (OperationCanceledException)
        {
            // Expected — either OperationCanceledException or its subtype TaskCanceledException.
        }
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// throws immediately when the cancellation token is already cancelled before the call.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Stream_WhenAlreadyCancelled_ShouldThrowImmediately(KnownAnswerTest kat)
    {
        using var source = new MemoryStream(kat.Input);
        using var target = new MemoryStream();
        using var transform = CreateTransform(kat);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        try
        {
            await transform.TransformAsync(source, target, bufferSize: kat.Input.Length, cts.Token);
            Assert.Fail($"[{kat.Name}] Expected OperationCanceledException.");
        }
        catch (OperationCanceledException)
        {
            // Expected.
        }
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// writes all transformed data to the target stream exclusively via the asynchronous write path,
    /// with no synchronous <see cref="Stream.Write(byte[],int,int)" /> calls.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Stream_WhenCompleted_ShouldWriteOnlyViaAsynchronousPath(KnownAnswerTest kat)
    {
        using var source = new MemoryStream(kat.Input);
        var trackingTarget = new DisposalTrackingStream(new MemoryStream());
        using var transform = CreateTransform(kat);

        await transform.TransformAsync(source, trackingTarget, bufferSize: kat.Input.Length);

        Assert.IsTrue(trackingTarget.AsyncWriteCount > 0,
            $"[{kat.Name}] Expected at least one asynchronous write to the target stream.");
        Assert.AreEqual(0, trackingTarget.SyncWriteCount,
            $"[{kat.Name}] No synchronous writes should be made to the target stream from an async method.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// does not dispose the target stream — neither synchronously nor asynchronously — after a
    /// successful transform, honouring the documented <c>leaveOpen</c> contract.
    /// </summary>
    [TestMethod]
    [DynamicData(nameof(GetValidTransformTestData))]
    public async Task TransformAsync_Stream_WhenCompleted_ShouldNotDisposeTargetStream(KnownAnswerTest kat)
    {
        using var source = new MemoryStream(kat.Input);
        var trackingTarget = new DisposalTrackingStream(new MemoryStream());
        using var transform = CreateTransform(kat);

        await transform.TransformAsync(source, trackingTarget, bufferSize: kat.Input.Length);

        Assert.IsFalse(trackingTarget.SyncDisposeCalled,
            $"[{kat.Name}] Target stream must not be synchronously disposed.");
        Assert.IsFalse(trackingTarget.AsyncDisposeCalled,
            $"[{kat.Name}] Target stream must not be asynchronously disposed.");
    }

    /// <summary>
    /// Verifies that <see cref="ICryptoTransformExtensions.TransformAsync(ICryptoTransform,Stream,Stream,int,CancellationToken)" />
    /// uses <see cref="IAsyncDisposable.DisposeAsync" /> rather than <see cref="IDisposable.Dispose" />
    /// when cleaning up the internal <see cref="CryptoStream" /> after a cancellation. This is
    /// observable because, when the operation is cancelled before <see cref="CryptoStream.FlushFinalBlockAsync" />
    /// runs, disposal triggers the final-block flush — writing asynchronously via <see cref="Stream.WriteAsync(ReadOnlyMemory{byte},CancellationToken)" />
    /// rather than synchronously via <see cref="Stream.Write(byte[],int,int)" />.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_WhenCancelledBeforeFlush_ShouldDisposeInternalStreamAsynchronously()
    {
        // Use PKCS7 padding (the default) so that disposal triggers a measurable final-block
        // write to the target stream even when no data has been fed to the transform.
        using var algorithm = CreateAlgorithm();
        using var encryptor = algorithm.CreateEncryptor();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var trackingTarget = new DisposalTrackingStream(new MemoryStream());
        using var source = new MemoryStream(new byte[] { 0x01 });

        try
        {
            await encryptor.TransformAsync(source, trackingTarget, bufferSize: 16, cts.Token);
        }
        catch (OperationCanceledException) { }

        Assert.AreEqual(0, trackingTarget.SyncWriteCount,
            "Disposal of the internal CryptoStream must not trigger synchronous writes; DisposeAsync must be used.");
        Assert.IsFalse(trackingTarget.SyncDisposeCalled,
            "The target stream must not be synchronously disposed (leaveOpen: true contract).");
        Assert.IsFalse(trackingTarget.AsyncDisposeCalled,
            "The target stream must not be asynchronously disposed (leaveOpen: true contract).");
    }

    /// <summary>
    /// Verifies that a round-trip using <see cref="ICryptoTransformExtensions.TransformAsync" /> for
    /// encryption followed by <see cref="ICryptoTransformExtensions.TransformAsync" /> for decryption
    /// produces the original plaintext.
    /// </summary>
    [TestMethod]
    public async Task TransformAsync_Stream_RoundTrip_ShouldProduceOriginalPlaintext()
    {
        using var algorithm = CreateAlgorithm();
        byte[] plainText = System.Text.Encoding.UTF8.GetBytes("round-trip-test");

        // Encrypt
        using var sourceStream = new MemoryStream(plainText);
        using var encryptedStream = new MemoryStream();
        using (var encryptor = algorithm.CreateEncryptor())
            await encryptor.TransformAsync(sourceStream, encryptedStream, bufferSize: 32);

        // Decrypt
        encryptedStream.Position = 0;
        using var decryptedStream = new MemoryStream();
        using (var decryptor = algorithm.CreateDecryptor())
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
    public async Task TransformAsync_Memory_WhenTransformIsNull_ShouldThrowArgumentNullException()
    {
        ICryptoTransform? transform = null;
        var input = new ReadOnlyMemory<byte>(new byte[] { 1, 2, 3, 4 });
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
        byte[] dest = new byte[kat.Input.Length + 16];
        using var transform = CreateTransform(kat);

        int written = await transform.TransformAsync(
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
        byte[] syncDest = new byte[kat.Input.Length + 16];
        int syncWritten;
        using (var syncTransform = CreateTransform(kat))
            syncWritten = syncTransform.Transform(kat.Input.AsSpan(), syncDest.AsSpan());

        byte[] asyncDest = new byte[kat.Input.Length + 16];
        int asyncWritten;
        using (var asyncTransform = CreateTransform(kat))
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
    public async Task TransformAsync_Memory_WhenAlreadyCancelled_ShouldThrowTaskCanceledException(
        KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);
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
    public async Task TransformAsync_Memory_WhenDestinationIsTooSmall_ShouldThrowArgumentException(
        KnownAnswerTest kat)
    {
        using var transform = CreateTransform(kat);

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
