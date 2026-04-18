using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Verifies that the algorithm's key remains unchanged after hashing.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void ComputeHash_WhenHashing_ShouldRespectKeyRetentionPolicy(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping valid key length validation.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);
            byte[] key = this.GenerateUniqueKey(specification.MinKeyLength);
            byte[] data = new byte[256];

            algorithm.Key = key;

            _ = algorithm.ComputeHash(data);

            // Validate key is unchanged
            if (algorithm.CanReuseTransform)

                // Reusable hash algorithm: key should remain unchanged
                CollectionAssert.AreEqual(key, algorithm.Key, "Key was unexpectedly modified by reusable algorithm.");
            else

                // One-shot: key should be cleared
                CollectionAssert.AreNotEqual(key, algorithm.Key, "Key was not cleared after one-shot MAC computation.");
        }

        /// <summary>
        /// Verifies that ComputeHash returns the same result when called multiple times with the same key and input.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void ComputeHash_WhenSameKeyAndInputUsed_ShouldReturnIdenticalResults(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping valid key length validation.");
                return;
            }

            byte[] key = this.GenerateUniqueKey(specification.MinKeyLength);
            byte[] data = new byte[256];

            using var algorithm1 = this.CreateAlgorithm(variant);
            using var algorithm2 = this.CreateAlgorithm(variant);
            algorithm1.Key = algorithm2.Key = key;

            byte[] hash1 = algorithm1.ComputeHash(data);
            byte[] hash2 = algorithm2.ComputeHash(data);

            CollectionAssert.AreEqual(hash1, hash2, "Hashes differ when using the same key and input.");
        }

        /// <summary>
        /// Verifies that ComputeHash returns different results when different keys are used with the same input.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void ComputeHash_WhenDifferentKeysUsed_ShouldReturnDifferentResults(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping valid key length validation.");
                return;
            }

            byte[] key1 = this.GenerateUniqueKey(specification.MinKeyLength);
            byte[] key2 = this.GenerateUniqueKey(specification.MinKeyLength);
            byte[] data = new byte[256];

            byte[] hash1, hash2;

            using var algorithm1 = this.CreateAlgorithm(variant);
            algorithm1.Key = key1;
            hash1 = algorithm1.ComputeHash(data);

            using var algorithm2 = this.CreateAlgorithm(variant);
            algorithm2.Key = key2;
            hash2 = algorithm2.ComputeHash(data);

            CollectionAssert.AreNotEqual(hash1, hash2);
        }
    }
}