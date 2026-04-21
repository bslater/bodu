// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NonCryptographicHashAlgorithmTests.AppendAsync.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.IO.Hashing;

namespace Bodu.IO.Hashing;

public abstract partial class NonCryptographicHashAlgorithmTests<TTest, TAlgorithm, TVariant>
{
    /// <summary>
    /// Verifies that <see cref="NonCryptographicHashAlgorithm.AppendAsync(Stream, CancellationToken)" />
    /// produces the same digest as the synchronous
    /// <see cref="NonCryptographicHashAlgorithm.Append(ReadOnlySpan{byte})" /> for the same input.
    /// </summary>
    /// <param name="variant">The algorithm variant under test.</param>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public async Task AppendAsync_WhenHashingStream_ShouldMatchSynchronousAppend(TVariant variant)
    {
        byte[] data = SharedInputs["Sequential_0_255"];

        NonCryptographicHashAlgorithm streaming = CreateAlgorithm(variant);
        using (MemoryStream source = new(data))
        {
            await streaming.AppendAsync(source).ConfigureAwait(false);
        }

        NonCryptographicHashAlgorithm synchronous = CreateAlgorithm(variant);
        synchronous.Append(data);

        CollectionAssert.AreEqual(synchronous.GetCurrentHash(), streaming.GetCurrentHash());
    }

    /// <summary>
    /// Verifies that passing a <see langword="null" /> stream to
    /// <see cref="NonCryptographicHashAlgorithm.AppendAsync(Stream, CancellationToken)" /> throws
    /// <see cref="ArgumentNullException" />.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task AppendAsync_WhenStreamIsNull_ShouldThrowArgumentNullException()
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm();

        await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
        {
            await algorithm.AppendAsync(null!).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// Verifies that a cancelled token passed to
    /// <see cref="NonCryptographicHashAlgorithm.AppendAsync(Stream, CancellationToken)" /> surfaces a
    /// <see cref="TaskCanceledException" /> without consuming the stream.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [TestMethod]
    public async Task AppendAsync_WhenCancellationRequested_ShouldThrowTaskCanceledException()
    {
        NonCryptographicHashAlgorithm algorithm = CreateAlgorithm();
        using MemoryStream stream = new(new byte[1024]);

        using CancellationTokenSource cts = new();
        cts.Cancel();

        await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
        {
            await algorithm.AppendAsync(stream, cts.Token).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }
}
