using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Bodu.Infrastructure;

namespace Bodu.Security.Cryptography
{
    public partial class ParallelMerkleTreeHashTests
    {
        // -----------------------------------------------------------------------------------------
        // Argument validation
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that a null stream throws <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenStreamIsNull_ShouldThrowExactly()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            await Assert.ThrowsExactlyAsync<ArgumentNullException>(async () =>
            {
                await hasher.ComputeHashAsync(null!);
            });
        }

        /// <summary>
        /// Verifies that calling ComputeHashAsync after Dispose throws <see cref="ObjectDisposedException"/>.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenInstanceIsDisposed_ShouldThrowExactly()
        {
            var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            hasher.Dispose();

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
            {
                await hasher.ComputeHashAsync(new MemoryStream(MakeData(4)));
            });
        }

        /// <summary>
        /// Verifies that hashing an empty stream throws <see cref="InvalidOperationException"/>
        /// because no leaf nodes were produced.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenStreamIsEmpty_ShouldThrowInvalidOperationException()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
            {
                await hasher.ComputeHashAsync(new MemoryStream());
            });
        }

        // -----------------------------------------------------------------------------------------
        // Return value
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that ComputeHashAsync returns a non-null result of the expected hash length.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenValidStreamProvided_ShouldReturnHashOfExpectedLength()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            byte[] result = await hasher.ComputeHashAsync(new MemoryStream(MakeData(8)));
            Assert.IsNotNull(result);
            Assert.AreEqual(4, result.Length);
        }

        // -----------------------------------------------------------------------------------------
        // Equivalence with sync overload
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that ComputeHashAsync and ComputeHash produce identical results for the same
        /// input data.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenSameInputAsSync_ShouldReturnIdenticalHash()
        {
            byte[] data = MakeData(21);

            using var async = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var sync = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            byte[] asyncResult = await async.ComputeHashAsync(new MemoryStream(data));
            byte[] syncResult = sync.ComputeHash(data);

            CollectionAssert.AreEqual(syncResult, asyncResult);
        }

        /// <summary>
        /// Verifies that a stream delivering data in partial chunks produces the same result as
        /// the sync span overload on the same bytes.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenStreamDeliversPartialReads_ShouldProduceSameResultAsSync()
        {
            const int length = 37;

            using var asyncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            using var syncHasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            // IncrementingByteStream delivers at most half its remaining bytes per Read call.
            using var stream = new IncrementingByteStream(length);
            byte[] asyncResult = await asyncHasher.ComputeHashAsync(stream);
            byte[] syncResult = syncHasher.ComputeHash(new IncrementingByteStream(length).ToArray());

            CollectionAssert.AreEqual(syncResult, asyncResult);
        }

        // -----------------------------------------------------------------------------------------
        // Cancellation
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that an already-cancelled token causes ComputeHashAsync to throw
        /// <see cref="TaskCanceledException"/> before completing.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenTokenAlreadyCancelled_ShouldThrowOTaskCanceledException()
        {
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            using var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
            {
                await hasher.ComputeHashAsync(new IncrementingByteStream(1024), cancellationToken: cts.Token);
            });
        }

        /// <summary>
        /// Verifies that cancelling the token mid-stream causes ComputeHashAsync to throw
        /// <see cref="TaskCanceledException"/>.
        /// </summary>
        [TestMethod]
        public async Task ComputeHashAsync_WhenTokenCancelledMidStream_ShouldThrowTaskCanceledException()
        {
            using var cts = new CancellationTokenSource();

            const int length = 1_000_000;
            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize: 256, fanOut: 2);

            // CancellationTriggerStream cancels the CTS after exactly 3 successful reads,
            // making the test deterministic — the next ReadAsync detects the cancelled token.
            using var stream = new CancellationTriggerStream(
                new IncrementingByteStream(length), cts, cancelAfterRead: 3);

            await Assert.ThrowsExactlyAsync<TaskCanceledException>(async () =>
            {
                await hasher.ComputeHashAsync(stream, cancellationToken: cts.Token);
            });
        }

        // -----------------------------------------------------------------------------------------
        // Various configurations
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that ComputeHashAsync produces the expected additive root across a range of
        /// block sizes and fan-out values.
        /// </summary>
        [TestMethod]
        [DataRow(4, 2, 8)]
        [DataRow(4, 2, 9)]
        [DataRow(4, 3, 12)]
        [DataRow(16, 2, 50)]
        public async Task ComputeHashAsync_WhenVariousConfigurationsUsed_ShouldMatchHandComputedRoot(
            int blockSize, int fanOut, int dataLength)
        {
            byte[] data = MakeData(dataLength);
            byte[] expected = ComputeAdditiveRoot(data, blockSize, fanOut);

            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
            byte[] actual = await hasher.ComputeHashAsync(new MemoryStream(data));

            CollectionAssert.AreEqual(expected, actual,
                $"Mismatch: blockSize={blockSize}, fanOut={fanOut}, length={dataLength}");
        }
    }
}