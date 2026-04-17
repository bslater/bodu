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
        /// Verifies that GenerateKey creates a key of expected length.
        /// </summary>
        [TestMethod]
        public void GenerateKey_WhenCalled_ShouldInitializeKeyCorrectly()
        {
            using var algorithm = CreateAlgorithm();
            int size = algorithm.LegalKeySizes[0].MinSize;
            algorithm.KeySize = size;
            algorithm.GenerateKey();

            Assert.AreEqual(size / 8, algorithm.Key.Length);
        }

        /// <summary>
        /// Validates that setting <see cref="SymmetricAlgorithm.GenerateKey" /> after the algorithm has been disposed throws
        /// an <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void GenerateKey_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
        {
            TAlgorithm algorithm = this.CreateAlgorithm();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                algorithm.GenerateKey();
            });
        }
    }
}