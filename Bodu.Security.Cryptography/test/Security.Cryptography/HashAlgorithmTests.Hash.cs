using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Verifies that accessing <see cref="HashAlgorithm.Hash" /> after calling <see cref="HashAlgorithm.Initialize" /> without
        /// finalizing the hash computation throws a <see cref="CryptographicUnexpectedOperationException" />.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(CryptographicUnexpectedOperationException))]
        public void Hash_Get_WhenInitializedAfterTransformBlock_ShouldThrowExactly()
        {
            using var algorithm = this.CreateAlgorithm();
            algorithm.TransformBlock(CryptoTestUtilities.ByteSequence0To255, 0, 256, null, 0);
            algorithm.Initialize();
            _ = algorithm.Hash;
        }

        /// <summary>
        /// Verifies that accessing <see cref="HashAlgorithm.Hash" /> without calling
        /// <see cref="HashAlgorithm.TransformFinalBlock(byte[], int, int)" /> throws a
        /// <see cref="CryptographicUnexpectedOperationException" />, as the hash is not finalized.
        /// </summary>
        /// <param name="offset">The starting position in the input buffer.</param>
        /// <param name="count">The number of bytes to process.</param>
        [DataTestMethod]
        [DataRow(0, 0)]
        [DataRow(0, 100)]
        [DataRow(10, 10)]
        [ExpectedException(typeof(CryptographicUnexpectedOperationException))]
        public void Hash_Get_WhenTransformFinalBlockNotCalled_ShouldThrowExactly(int offset, int count)
        {
            using var algorithm = this.CreateAlgorithm();
            algorithm.TransformBlock(CryptoTestUtilities.ByteSequence0To255, offset, count, null, 0);
            _ = algorithm.Hash;
        }
    }
}