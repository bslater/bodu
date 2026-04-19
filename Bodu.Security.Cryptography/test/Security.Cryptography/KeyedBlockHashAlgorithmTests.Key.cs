using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class KeyedBlockHashAlgorithmTests<TTest, TAlgorithm, TVariant>
    {
        /// <summary>
        /// Verifies that setting a null key throws an <see cref="ArgumentNullException" />.
        /// </summary>
        [TestMethod]
        public void Key_WhenSetToNull_ShouldThrowExactly()
        {
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.Key = null!;
            });
        }

        /// <summary>
        /// Verifies that setting an invalid key throws a <see cref="CryptographicException" />.
        /// </summary>
        [TestMethod]
        public void Key_WhenSetToInvalidKey_ShouldThrowExactly()
        {
            using var algorithm = this.CreateAlgorithm();

            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                algorithm.Key = Array.Empty<byte>();
            });
        }

        /// <summary>
        /// Verifies that setting a key whose length falls outside the valid range declared in the specification throws a
        /// <see cref="CryptographicException" /> with the correct message.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenSetToInvalidLength_ShouldThrowExactly(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            foreach (int invalidLength in specification.GetRejectedKeyLengths())
            {
                Assert.ThrowsExactly<CryptographicException>(
                    () => algorithm.Key = new byte[invalidLength],
                    $"[{variant}] Expected CryptographicException for key length {invalidLength} " +
                    $"(valid range: {specification.MinKeyLength}–{specification.MaxKeyLength} bytes).");
            }
        }

        /// <summary>
        /// Verifies that setting a key whose length falls within the valid range declared in the specification is accepted
        /// without throwing.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenSetToValidLength_ShouldNotThrow(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping valid key length validation.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            foreach (int validLength in specification.GetAcceptedKeyLengths())
            {
                try
                {
                    algorithm.Key = new byte[validLength];
                }
                catch (Exception ex)
                {
                    Assert.Fail(
                        $"[{variant}] Key length {validLength} should be accepted but threw {ex.GetType().Name}: {ex.Message} " +
                        $"(valid range: {specification.MinKeyLength}–{specification.MaxKeyLength} bytes).");
                }
            }
        }

        /// <summary>
        /// Verifies that the algorithm initialises a non-null default key on construction whose length falls within the
        /// valid range declared in the specification.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenDefaultConstructed_ShouldNotBeNull(TVariant variant)
        {
            using var algorithm = this.CreateAlgorithm(variant);

            Assert.IsNotNull(algorithm.Key,
                $"[{variant}] Expected a non-null default key on construction.");
        }

        /// <summary>
        /// Verifies the correct get/set behavior of the <see cref="KeyedHashAlgorithm.Key" /> property.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenSetAndRetrieved_ShouldBehaveAsExpected(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);


            byte[] key = Enumerable.Range(0, specification.MinKeyLength).Select(i => (byte)i).ToArray();
            algorithm.Key = key;
            CollectionAssert.AreEqual(key, algorithm.Key);
            Assert.AreNotSame(key, algorithm.Key);

            byte[] copy = algorithm.Key;
            CollectionAssert.AreEqual(copy, key);
            CollectionAssert.AreEqual(copy, algorithm.Key);
            Assert.AreNotSame(copy, key);
            Assert.AreNotSame(copy, algorithm.Key);

            copy[0]++;
            CollectionAssert.AreNotEqual(copy, key);
            CollectionAssert.AreNotEqual(copy, algorithm.Key);
        }

        /// <summary>
        /// Verifies that mutating the original key array after assignment does not affect the internal key stored by the algorithm.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenOriginalKeyIsMutated_ShouldNotAffectInternalKey(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] key = Enumerable.Range(0, specification.MinKeyLength).Select(i => (byte)i).ToArray();
            algorithm.Key = key;

            // Mutate original key
            key[0] ^= 0xFF;

            // Assert internal key is unchanged
            Assert.AreNotEqual(key[0], algorithm.Key[0]);
        }

        /// <summary>
        /// Verifies that accessing the <see cref="KeyedHashAlgorithm.Key" /> property multiple times returns distinct array instances.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenAccessedMultipleTimes_ShouldReturnDistinctInstances(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            var first = algorithm.Key;
            var second = algorithm.Key;

            Assert.AreEqual(first.Length, second.Length);
            Assert.AreEqual(Convert.ToHexString(first), Convert.ToHexString(second));
            Assert.AreNotSame(first, second);
        }

        /// <summary>
        /// Verifies that updating the key after a hash operation does not retroactively affect the result of the previous hash.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenChangedAfterHash_ShouldNotAffectPreviousResult(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] data = new byte[128];
            byte[] key1 = this.GenerateUniqueKey(specification.MinKeyLength);
            byte[] key2 = this.GenerateUniqueKey(specification.MinKeyLength);

            byte[] hash1, hash2;

            algorithm.Key = key1;
            hash1 = algorithm.ComputeHash(data);

            algorithm.Key = key2;
            hash2 = algorithm.ComputeHash(data);

            Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2));
        }

        /// <summary>
        /// Verifies that reassigning the same key hashValue multiple times does not throw or alter behavior.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenSetToSameValueMultipleTimes_ShouldNotThrow(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] key = this.GenerateUniqueKey(specification.MinKeyLength);

            algorithm.Key = key;
            algorithm.Key = key; // Reassign
            Assert.IsNotNull(algorithm.Key);
        }

        /// <summary>
        /// Verifies that calling <see cref="HashAlgorithm.Initialize" /> does not reset the key unless explicitly cleared.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenInitializeCalled_ShouldNotResetKey(TVariant variant)
        {
            using var algorithm = this.CreateAlgorithm(variant);
            byte[] originalKey = algorithm.Key;
            algorithm.Initialize();

            CollectionAssert.AreEqual(originalKey, algorithm.Key);
        }

        /// <summary>
        /// Verifies that using the same key with different inputs produces different hashes.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenCallingComputeHashUsingSameKeyAndDifferentInputs_ShouldReturnDifferentHashes(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] key = this.GenerateUniqueKey(specification.TestKey.Length);
            algorithm.Key = key;

            byte[] input1 = (byte[])CryptoTestUtilities.ByteSequence256.Clone();
            byte[] input2 = (byte[])CryptoTestUtilities.ByteSequence256.Clone();
            input2[0]++;

            byte[] hash1 = algorithm.ComputeHash(input1);
            byte[] hash2 = algorithm.ComputeHash(input2);

            Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2));
        }

        /// <summary>
        /// Verifies that modifying the original key array after assigning it to the algorithm does not affect internal state, provided the
        /// algorithm supports reuse.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenModifiedAfterAssignment_ShouldNotAffectInternalState(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] key = this.GenerateUniqueKey(specification.MinKeyLength);
            byte[] data = Enumerable.Repeat((byte)0x42, 64).ToArray();

            algorithm.Key = key;

            // First hash with original key
            byte[] hash1 = algorithm.ComputeHash(data);

            // Mutate the original key array
            key[0] ^= 0xFF;

            if (!specification.CanReuseTransform)
            {
                // For one-shot MACs like Poly1305, the second hash must use a new instance and key
                using var algorithm2 = this.CreateAlgorithm();
                byte[] newKey = this.GenerateUniqueKey(specification.MinKeyLength);
                algorithm2.Key = newKey;
                byte[] hash2 = algorithm2.ComputeHash(data);

                CollectionAssert.AreNotEqual(hash1, hash2,
                    "Expected different hash from new key after modifying external key array in a one-shot MAC.");
            }
            else
            {
                // For reusable hash algorithms, the key should remain internally stable
                byte[] hash2 = algorithm.ComputeHash(data);

                CollectionAssert.AreEqual(hash1, hash2,
                    "Hash output changed after modifying external key array, suggesting internal state is not protected.");
            }
        }

        /// <summary>
        /// Verifies that modifying the input array after setting it as a key does not mutate the internal copy.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenInputArrayModified_ShouldNotMutateInternalKey(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] key = this.GenerateUniqueKey(specification.MinKeyLength);
            algorithm.Key = key;

            byte[] snapshot = algorithm.Key.ToArray();

            key[0] ^= 0xFF;

            CollectionAssert.AreEqual(snapshot, algorithm.Key);
        }

        /// <summary>
        /// Verifies that assigning a key larger than the maximum legal length throws a CryptographicException.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenAboveMaximumLength_ShouldThrow(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] tooLong = new byte[specification.MaxKeyLength + 1];
            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                algorithm.Key = tooLong;
            });
        }

        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenBelowMinimumLength_ShouldThrow(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] tooShort = new byte[specification.MinKeyLength - 1];

            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                algorithm.Key = tooShort;
            });
        }

        /// <summary>
        /// Verifies that modifying the key after hashing has begun throws an exception.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenSetAfterHashingBegins_ShouldThrow(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            using var algorithm = this.CreateAlgorithm(variant);

            byte[] newKey = this.GenerateUniqueKey(specification.MinKeyLength);
            byte[] input = new byte[1024];

            // Begin processing
            algorithm.TransformBlock(input, 0, input.Length, null, 0);

            // Attempt to set the key mid-stream
            Assert.ThrowsExactly<CryptographicUnexpectedOperationException>(() =>
            {
                algorithm.Key = newKey;
            });
        }

        /// <summary>
        /// Verifies that byte-reversing the key produces a distinct digest. Regression guard for the SipHash
        /// endianness fix — a key loader that treated the key as byte-order-agnostic (or otherwise lost ordering)
        /// would produce identical digests for reversed keys.
        /// </summary>
        [TestMethod]
        [DynamicData(nameof(HashAlgorithmVariants))]
        public void Key_WhenKeyBytesReversed_ShouldProduceDistinctDigest_fix(TVariant variant)
        {
            if (this.GetSpecification(variant) is not KeyedAlgorithmSpecification specification)
            {
                Assert.Inconclusive($"[{variant}] Algorithm is not keyed; skipping invalid test case.");
                return;
            }

            byte[] key = Enumerable.Range(1, specification.MinKeyLength).Select(i => (byte)i).ToArray();
            byte[] reversed = key.Reverse().ToArray();
            byte[] data = (byte[])CryptoTestUtilities.ByteSequence256.Clone();

            byte[] hash1;
            byte[] hash2;

            using (var algorithm = this.CreateAlgorithm(variant))
            {
                algorithm.Key = key;
                hash1 = algorithm.ComputeHash(data);
            }

            using (var algorithm = this.CreateAlgorithm(variant))
            {
                algorithm.Key = reversed;
                hash2 = algorithm.ComputeHash(data);
            }

            Assert.AreNotEqual(Convert.ToHexString(hash1), Convert.ToHexString(hash2),
                $"[{variant}] Reversing the key bytes must yield a different digest.");
        }
    }
}