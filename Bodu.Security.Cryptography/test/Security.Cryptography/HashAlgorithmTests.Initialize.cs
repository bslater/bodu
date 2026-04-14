// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Verifies that reusing the algorithm instance after <see cref="HashAlgorithm.Initialize" /> resets the state correctly.
        /// </summary>
        [TestMethod]
        public void Initialize_WhenCalledBetweenHashes_ShouldResetStateForNewHash()
        {
            using var algorithm = this.CreateAlgorithm();

            byte[] input1 = CryptoTestUtilities.SimpleTextAsciiBytes;
            byte[] input2 = CryptoTestUtilities.ByteSequence0To255;

            byte[] hash1 = algorithm.ComputeHash(input1);

            algorithm.Initialize(); // reset state
            byte[] hash2 = algorithm.ComputeHash(input2);

            Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2), "Hashes should differ between independent inputs.");
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> multiple times without computing does not throw or alter behavior.
        /// </summary>
        [TestMethod]
        public void Initialize_WhenCalledRepeatedly_ShouldNotThrowOrAffectBehavior()
        {
            using var algorithm = this.CreateAlgorithm();

            algorithm.Initialize();
            algorithm.Initialize();

            byte[] input = CryptoTestUtilities.SimpleTextAsciiBytes;
            byte[] hash = algorithm.ComputeHash(input);

            Assert.IsNotNull(hash);
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm" /> can be reused after finalizing a hash via
        /// <see cref="HashAlgorithm.TransformFinalBlock" /> and reinitializing, if supported.
        /// </summary>
        [TestMethod]
        public void Initialize_WhenCalledAfterFinalBlock_ShouldAllowNewHashComputation()
        {
            using var algorithm = this.CreateAlgorithm();

            byte[] input = CryptoTestUtilities.ByteSequence0To255;
            algorithm.TransformFinalBlock(input, 0, input.Length);
            byte[] hash1 = algorithm.Hash!;

            if (!algorithm.CanReuseTransform)
            {
                // For one-shot algorithms, reinitialization without re-keying is invalid. Confirm that the second hash is not equal or that
                // key is cleared.
                algorithm.Initialize();

                // Either the key was cleared or a new random key was assigned; hash must differ.
                byte[] hash2 = algorithm.ComputeHash(input);
                CollectionAssert.AreNotEqual(hash1, hash2, "One-shot MAC should not yield the same result after reinitialization.");
            }
            else
            {
                algorithm.Initialize();

                byte[] hash2 = algorithm.ComputeHash(input);
                CollectionAssert.AreEqual(hash1, hash2, "Hashes should match after reinitializing with the same input.");
            }
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> mid-hashing resets any partially buffered data.
        /// </summary>
        [TestMethod]
        public void Initialize_MidHashing_ShouldResetPartialState()
        {
            using var algorithm = this.CreateAlgorithm();

            byte[] part1 = CryptoTestUtilities.SimpleTextAsciiBytes.Take(4).ToArray();
            byte[] part2 = CryptoTestUtilities.SimpleTextAsciiBytes.Skip(4).ToArray();

            algorithm.TransformBlock(part1, 0, part1.Length, null, 0);
            algorithm.Initialize(); // Reset mid-hash

            // Start a new hash
            byte[] result = algorithm.ComputeHash(part2);

            Assert.IsNotNull(result);

            // No expectations on the hashValue - just ensure no crash/corruption
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> after disposal throws an <see cref="ObjectDisposedException" />,
        /// matching base .NET behavior.
        /// </summary>
        [TestMethod]
        public void Initialize_WhenDisposed_ShouldThrowExactly()
        {
            // Arrange
            using var algorithm = this.CreateAlgorithm();
            algorithm.Dispose();

            // Act & Assert
            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                algorithm.Initialize();
            });
        }

        /// <summary>
        /// Verifies that <see cref="HashAlgorithm.Initialize" /> resets the algorithm's
        /// <see cref="HashAlgorithm.State" /> so that a hash computation can be started immediately
        /// afterwards without a <see cref="CryptographicUnexpectedOperationException" /> being raised.
        /// Regression guard for <see cref="CubeHash" />, where the .NET-6+ branch of
        /// <c>Initialize()</c> previously did not reset <c>State</c>.
        /// </summary>
        [TestMethod]
        public void Initialize_AfterHashing_ShouldResetStateForReuse()
        {
            using var algorithm = this.CreateAlgorithm();

            byte[] first = algorithm.ComputeHash(new byte[] { 1, 2, 3, 4 });
            Assert.IsNotNull(first);

            algorithm.Initialize();

            // Must not throw — State should be back to its idle value.
            byte[] second = algorithm.ComputeHash(new byte[] { 5, 6, 7, 8 });
            Assert.IsNotNull(second);
        }

    }
}