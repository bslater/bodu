using Bodu.Infrastructure;
using System;
using System.Security.Cryptography;
using System.Threading;

namespace Bodu.Security.Cryptography
{
    public partial class ParallelMerkleTreeHashTests
    {
        /// <summary>
        /// Verifies that a null algorithm factory throws <see cref="ArgumentNullException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenAlgorithmFactoryIsNull_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new ParallelMerkleTreeHash(null!);
            });
        }

        /// <summary>
        /// Verifies that a block size of zero throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenBlockSizeIsZero_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = new ParallelMerkleTreeHash(Factory, blockSize: 0);
            });
        }

        /// <summary>
        /// Verifies that a negative block size throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        public void Constructor_WhenBlockSizeIsNegative_ShouldThrowExactly(int blockSize)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = new ParallelMerkleTreeHash(Factory, blockSize: blockSize);
            });
        }

        /// <summary>
        /// Verifies that a fan-out of one throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenFanOutIsOne_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = new ParallelMerkleTreeHash(Factory, fanOut: 1);
            });
        }

        /// <summary>
        /// Verifies that a fan-out of zero throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenFanOutIsZero_ShouldThrowExactly()
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = new ParallelMerkleTreeHash(Factory, fanOut: 0);
            });
        }

        /// <summary>
        /// Verifies that a negative fan-out throws <see cref="ArgumentOutOfRangeException"/>.
        /// </summary>
        [TestMethod]
        [DataRow(-1)]
        [DataRow(int.MinValue)]
        public void Constructor_WhenFanOutIsNegative_ShouldThrowExactly(int fanOut)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = new ParallelMerkleTreeHash(Factory, fanOut: fanOut);
            });
        }

        /// <summary>
        /// Verifies that construction with a valid factory and default parameters succeeds.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenValidFactoryProvided_ShouldSucceed()
        {
            using var hasher = new ParallelMerkleTreeHash(Factory);
            Assert.IsNotNull(hasher);
        }

        /// <summary>
        /// Verifies that construction with explicit valid parameters succeeds.
        /// </summary>
        [TestMethod]
        [DataRow(1, 2)]
        [DataRow(4, 2)]
        [DataRow(4, 3)]
        [DataRow(1024, 4)]
        public void Constructor_WhenValidParametersProvided_ShouldSucceed(int blockSize, int fanOut)
        {
            using var hasher = new ParallelMerkleTreeHash(Factory, blockSize, fanOut);
            Assert.IsNotNull(hasher);
        }

        /// <summary>
        /// Verifies that each factory invocation produces a distinct algorithm instance, ensuring
        /// no algorithm state is shared between concurrent level workers.
        /// </summary>
        [TestMethod]
        public void Constructor_WhenFactoryInvokedRepeatedly_ShouldReturnDistinctInstances()
        {
            int callCount = 0;
            HashAlgorithm? first = null;
            HashAlgorithm? second = null;

            Func<HashAlgorithm> countingFactory = () =>
            {
                var instance = new MonitoringHashAlgorithm();
                if (callCount++ == 0) first = instance;
                else second ??= instance;
                return instance;
            };

            using var hasher = new ParallelMerkleTreeHash(countingFactory, blockSize: 4, fanOut: 2);
            hasher.ComputeHash(MakeData(8));

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreNotSame(first, second);
        }
    }
}