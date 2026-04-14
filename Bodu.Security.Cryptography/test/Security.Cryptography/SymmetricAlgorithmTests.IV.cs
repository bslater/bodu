using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public abstract partial class SymmetricAlgorithmTests<TAlgorithm>
    {
        /// <summary>
        /// Validates that the IV property is not null upon algorithm creation.
        /// </summary>
        [TestMethod]
        public void IV_WhenAccessed_ShouldNotBeNull()
        {
            using TAlgorithm algorithm = this.CreateAlgorithm();
            Assert.IsNotNull(algorithm.IV);
        }

        /// <summary>
        /// Validates that accessing the IV after disposing the algorithm returns a different hashValue.
        /// </summary>
        [TestMethod]
        public void IV_WhenAccessedAfterDispose_ShouldReturnDifferentValue()
        {
            TAlgorithm algorithm = this.CreateAlgorithm();
            byte[] ivBeforeDispose = algorithm.IV;
            algorithm.Dispose();
            byte[] ivAfterDispose = algorithm.IV;
            CollectionAssert.AreNotEqual(ivBeforeDispose, ivAfterDispose);
        }

        /// <summary>
        /// Validates that setting the IV to null throws an ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void IV_WhenSetToNull_ShouldThrowExactly()
        {
            using TAlgorithm algorithm = this.CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() => algorithm.IV = null);
        }

        /// <summary>
        /// Validates that setting an invalid IV size throws a CryptographicException.
        /// </summary>
        [TestMethod]
        public void IV_WhenSetToInvalidSize_ShouldThrowExactly()
        {
            using TAlgorithm algorithm = this.CreateAlgorithm();
            byte[] invalidIV = new byte[algorithm.BlockSize - 1];
            Assert.ThrowsExactly<CryptographicException>(() => algorithm.IV = invalidIV);
        }

        /// <summary>
        /// Verifies that setting IV returns the same hashValue on subsequent get.
        /// </summary>
        [TestMethod]
        public void IV_WhenSet_ShouldReturnSameValueOnGet()
        {
            using TAlgorithm algorithm = this.CreateAlgorithm();
            byte[] iv = new byte[algorithm.BlockSize / 8];
            CryptoHelpers.FillWithRandomNonZeroBytes(iv);

            algorithm.IV = iv;
            CollectionAssert.AreEqual(iv, algorithm.IV);
        }

        /// <summary>
        /// Verifies that the IV property returns a defensive copy (not the same reference).
        /// </summary>
        [TestMethod]
        public void IV_WhenSet_ShouldReturnDefensiveCopy()
        {
            using TAlgorithm algorithm = this.CreateAlgorithm();
            byte[] iv = new byte[algorithm.BlockSize / 8];
            CryptoHelpers.FillWithRandomNonZeroBytes(iv);

            algorithm.IV = iv;
            Assert.AreNotSame(iv, algorithm.IV);
        }

        /// <summary>
        /// Verifies that modifying a retrieved IV does not affect the internal state.
        /// </summary>
        [TestMethod]
        public void IV_WhenModifiedAfterGet_ShouldNotAffectInternalState()
        {
            using TAlgorithm algorithm = this.CreateAlgorithm();
            byte[] iv = new byte[algorithm.BlockSize / 8];
            CryptoHelpers.FillWithRandomNonZeroBytes(iv);

            algorithm.IV = iv;
            byte[] ivCopy = algorithm.IV;
            ivCopy[0]++; // mutate

            CollectionAssert.AreNotEqual(ivCopy, algorithm.IV);
        }

        /// <summary>
        /// Verifies that GenerateIV produces a different IV from the previous one.
        /// </summary>
        [TestMethod]
        public void GenerateIV_WhenCalled_ShouldChangeIV()
        {
            using TAlgorithm algorithm = this.CreateAlgorithm();
            byte[] initialIV = algorithm.IV;

            algorithm.GenerateIV();
            CollectionAssert.AreNotEqual(initialIV, algorithm.IV);
        }

        /// <summary>
        /// Verifies that creating an encryptor with a wrong-length IV throws
        /// <see cref="CryptographicException" /> whose message reports the offending IV bit-length
        /// rather than an unrelated value (e.g. the key length). Regression guard for a copy-paste
        /// in <see cref="Threefish" />'s validation diagnostics.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WithInvalidIvLength_ShouldThrowArgumentException()
        {
            using var algorithm = this.CreateAlgorithm();
            algorithm.GenerateKey();

            int blockSizeBytes = algorithm.BlockSize / 8;
            byte[] badIv = new byte[blockSizeBytes - 1];
            int expectedBitLength = badIv.Length * 8;

            var ex = Assert.ThrowsExactly<ArgumentException>(() =>
            {
                using var _ = algorithm.CreateEncryptor(algorithm.Key, badIv);
            });

            Assert.IsTrue(
                ex.Message.Contains(expectedBitLength.ToString()),
                $"Expected IV bit-length {expectedBitLength} in message but got: {ex.Message}");
        }

    }
}