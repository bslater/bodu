using System;
using System.IO;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public partial class ParallelMerkleTreeHashTests
    {
        /// <summary>
        /// Verifies that calling Dispose twice does not throw.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalledTwice_ShouldNotThrow()
        {
            var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            hasher.Dispose();
            hasher.Dispose();
        }

        /// <summary>
        /// Verifies that the instance can be used within a using statement without error.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenUsedWithUsingStatement_ShouldDisposeCleanly()
        {
            byte[] result;
            using (var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut))
                result = hasher.ComputeHash(MakeData(4));
            Assert.IsNotNull(result);
        }

        /// <summary>
        /// Verifies that ComputeHash(ReadOnlySpan) after Dispose throws <see cref="ObjectDisposedException"/>.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenComputeHashCalledAfterDispose_ShouldThrowExactly()
        {
            var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            hasher.Dispose();
            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                hasher.ComputeHash(MakeData(4).AsSpan());
            });
        }

        /// <summary>
        /// Verifies that ComputeHashAsync after Dispose throws <see cref="ObjectDisposedException"/>.
        /// </summary>
        [TestMethod]
        public async Task Dispose_WhenComputeHashAsyncCalledAfterDispose_ShouldThrowExactly()
        {
            var hasher = new ParallelMerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            hasher.Dispose();

            await Assert.ThrowsExactlyAsync<ObjectDisposedException>(async () =>
            {
                await hasher.ComputeHashAsync(new MemoryStream(MakeData(4)));
            });
        }
    }
}