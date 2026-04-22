// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.Tweak.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that accessing <see cref="TweakableSymmetricAlgorithm.Tweak" /> after disposal throws <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenAccessedAfterDispose_ShouldThrowObjectDisposedException()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
            algorithm.GenerateTweak();

            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                var _ = algorithm.Tweak;
            });
        }

        /// <summary>
        /// Verifies that each access to <see cref="TweakableSymmetricAlgorithm.Tweak" /> returns a new array instance.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenCalledMultipleTimes_ShouldReturnNewArrayInstances()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
            algorithm.GenerateTweak();

            var tweak1 = algorithm.Tweak;
            var tweak2 = algorithm.Tweak;

            Assert.AreNotSame(tweak1, tweak2);
        }

        /// <summary>
        /// Verifies that each access to <see cref="TweakableSymmetricAlgorithm.Tweak" /> returns arrays with the same contents.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenCalledMultipleTimes_ShouldReturnSameValue()
        {
            using TAlgorithm algorithm = CreateAlgorithm();
            algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
            algorithm.GenerateTweak();

            var tweak1 = algorithm.Tweak;
            var tweak2 = algorithm.Tweak;

            CollectionAssert.AreEqual(tweak1, tweak2);
        }

        /// <summary>
        /// Verifies that GenerateTweak produces a non-zero value that is preserved by the Tweak property.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenGenerated_ShouldMatchInternalTweakValue()
        {
            using var algorithm = CreateAlgorithm();
            int size = algorithm.LegalTweakSizes[0].MinSize;

            algorithm.TweakSize = size;
            algorithm.GenerateTweak();

            byte[] tweak = algorithm.Tweak;
            Assert.AreEqual(size / 8, tweak.Length);
            Assert.IsTrue(tweak.Any(b => b != 0), "Generated tweak should not be all zero.");
        }

        /// <summary>
        /// Verifies that accessing Tweak before it is initialized throws.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenNoAccessedMultipleTimes_ShouldReturnSameValue()
        {
            using var algorithm = CreateAlgorithm();

            byte[] tweak1 = algorithm.Tweak;
            byte[] tweak2 = algorithm.Tweak;

            CollectionAssert.AreEqual(tweak1, tweak2);
        }

        /// <summary>
        /// Verifies that accessing Tweak before it is initialized throws.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenNotInitialized_ShouldReturnExpectedValue()
        {
            using var algorithm = CreateAlgorithm();

            byte[] tweak = algorithm.Tweak;
            Assert.AreEqual(algorithm.TweakSize / 8, tweak.Length);
            Assert.IsTrue(tweak.Any(b => b != 0), "Generated tweak should not be all zero.");
        }

        /// <summary>
        /// Verifies that once set, the Tweak value does not change unless reassigned or reset.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenNotReassigned_ShouldRemainUnchanged()
        {
            using var algorithm = CreateAlgorithm();
            int size = algorithm.LegalTweakSizes[0].MinSize;
            byte[] expected = Enumerable.Repeat((byte)0xAA, size / 8).ToArray();

            algorithm.TweakSize = size;
            algorithm.Tweak = expected;

            byte[] first = algorithm.Tweak;
            byte[] second = algorithm.Tweak;

            CollectionAssert.AreEqual(first, second);
        }

        /// <summary>
        /// Verifies that setting the Tweak returns the exact same content when retrieved.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenSet_ShouldReturnExpectedValue()
        {
            using var algorithm = CreateAlgorithm();
            int size = algorithm.LegalTweakSizes[0].MinSize;
            byte[] expected = Enumerable.Range(0, size / 8).Select(i => (byte)(i + 1)).ToArray();

            algorithm.TweakSize = size;
            algorithm.Tweak = expected;
            byte[] actual = algorithm.Tweak;

            CollectionAssert.AreEqual(expected, actual);
        }

        /// <summary>
        /// Verifies that Tweak set to an invalid size throws.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenSetToInvalidSize_ShouldThrowCryptographicException()
        {
            using var algorithm = CreateAlgorithm();
            var invalid = new byte[7]; // 56 bits is uncommon

            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                algorithm.Tweak = invalid;
            });
        }

        /// <summary>
        /// Verifies that Tweak set to null throws ArgumentNullException.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenSetToNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.Tweak = null!;
            });
        }

        /// <summary>
        /// Verifies that setting <see cref="TweakableSymmetricAlgorithm.Tweak" /> to a wrongly-sized
        /// byte array throws <see cref="CryptographicException" /> whose message does not reference
        /// the unrelated literal "TweakSchedule" that the original
        /// <c>[CallerArgumentExpression]</c> declared.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenSetToInvalidSize_ShouldThrowWithoutReferencingTweakSchedule()
        {
            using var algo = Threefish256.Create();

            var ex = Assert.ThrowsExactly<CryptographicException>(() =>
            {
                algo.Tweak = new byte[5];
            });

            Assert.IsFalse(string.IsNullOrEmpty(ex.Message), "Expected non-empty exception message.");
            Assert.IsFalse(ex.Message.Contains("TweakSchedule"),
                "Exception message must not reference the unrelated 'TweakSchedule' symbol.");
        }

        /// <summary>
        /// Verifies that setting <see cref="TweakableSymmetricAlgorithm.Tweak" /> to a wrongly-sized
        /// byte array throws <see cref="CryptographicException" /> whose message contains no
        /// rename-refactor artefacts (such as the unrelated literal <c>"TweakSchedule"</c> that the
        /// original <c>[CallerArgumentExpression]</c> attribute on
        /// <c>ThrowIfInvalidTweakSize</c> referenced).
        /// </summary>
        [TestMethod]
        public void Tweak_WhenSetToInvalidSize_ShouldThrowWithCleanMessage()
        {
            using var algorithm = CreateAlgorithm();

            var ex = Assert.ThrowsExactly<CryptographicException>(() =>
            {
                algorithm.Tweak = new byte[5];

            });

            Assert.IsFalse(string.IsNullOrEmpty(ex.Message), "Expected non-empty exception message.");
            Assert.IsFalse(ex.Message.Contains("TweakSchedule"),
                "Exception message must not reference the unrelated 'TweakSchedule' symbol.");
            Assert.IsFalse(ex.Message.Contains("this."),
                "Exception message must not contain stray 'this.' refactor artefacts.");
        }

        /// <summary>
        /// Verifies that assigning a tweak whose length in bits is not among
        /// <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" /> throws
        /// <see cref="CryptographicException" />.
        /// Skips with <see cref="Assert.Inconclusive" /> when the algorithm accepts every
        /// byte-aligned length and no invalid size can be constructed.
        /// </summary>
        [TestMethod]
        public void Tweak_WhenSetToInvalidSize_ShouldThrowExactly()
        {
            using var algorithm = CreateAlgorithm();

            int? invalidBits = FindInvalidTweakSize(algorithm.LegalTweakSizes);

            if (invalidBits is null)
            {
                Assert.Inconclusive(
                    $"{typeof(TAlgorithm).Name} accepts every byte-aligned tweak length — " +
                    "no invalid size can be constructed for this test.");
                return;
            }

            byte[] invalidTweak = new byte[invalidBits.Value / 8];

            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                algorithm.Tweak = invalidTweak;
            },
                $"Setting a {invalidBits.Value}-bit tweak should throw CryptographicException " +
                $"for {typeof(TAlgorithm).Name}.");
        }
    }
}