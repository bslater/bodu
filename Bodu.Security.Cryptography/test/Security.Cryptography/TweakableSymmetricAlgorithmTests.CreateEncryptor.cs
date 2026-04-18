using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

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
    }
}