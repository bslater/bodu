// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TweakableSymmetricAlgorithmTests.CreateEncryptor.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when the IV is null.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenIVIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.CreateEncryptor(new byte[algorithm.KeySize / 8], null!, new byte[algorithm.TweakSize / 8]);
            });
        }

        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when the key is null.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenKeyIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.CreateEncryptor(null!, new byte[algorithm.BlockSize / 8], new byte[algorithm.TweakSize / 8]);
            });
        }

        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when the tweak is null.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenTweakIsNull_ShouldThrowArgumentNullException()
        {
            using var algorithm = CreateAlgorithm();
            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                algorithm.CreateEncryptor(new byte[algorithm.KeySize / 8], new byte[algorithm.BlockSize / 8], null!);
            });
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor(byte[], byte[])" /> uses the configured tweak.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WithKeyAndIV_ShouldUseConfiguredTweak()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
            algorithm.GenerateTweak();

            using var decryptor = algorithm.CreateEncryptor(algorithm.Key, algorithm.IV);
            Assert.IsNotNull(decryptor);
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithm.CreateDecryptor()" /> uses the configured tweak.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WithoutParameters_ShouldUseConfiguredTweak()
        {
            using var algorithm = CreateAlgorithm();
            algorithm.TweakSize = algorithm.LegalTweakSizes[0].MinSize;
            algorithm.GenerateTweak();

            using var decryptor = algorithm.CreateEncryptor();
            Assert.IsNotNull(decryptor);
        }

        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.CreateEncryptor(byte[], byte[], byte[])" /> throws
        /// <see cref="CryptographicException" /> (not <see cref="ArgumentException" />) when the IV length does not
        /// match the configured block size. Regression guard for the Threefish IV validation branch previously
        /// throwing <see cref="ArgumentException" /> inconsistently with the key and tweak branches.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenIvLengthIsInvalid_ShouldThrowCryptographicException_fix()
        {
            using var algorithm = CreateAlgorithm();

            byte[] key = new byte[algorithm.KeySize / 8];
            byte[] tweak = new byte[algorithm.TweakSize / 8];
            byte[] badIv = new byte[(algorithm.BlockSize / 8) + 1];

            Assert.ThrowsExactly<CryptographicException>(() =>
            {
                using var _ = algorithm.CreateEncryptor(key, badIv, tweak);
            });
        }
    }
}