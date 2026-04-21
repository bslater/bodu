using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class SymmetricAlgorithmTests<TAlgorithm>
    {
        /// <summary>
        /// Validates that setting <see cref="SymmetricAlgorithm.CreateEncryptor" /> after the algorithm has been disposed throws
        /// an <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
        {
            TAlgorithm algorithm = CreateAlgorithm();
            algorithm.Dispose();

            Assert.ThrowsExactly<ObjectDisposedException>(() =>
            {
                _ = algorithm.CreateEncryptor();
            });
        }

        /// <summary>
        /// Verifies that attempting to create a cryptographic transform on a disposed
        /// <typeparamref name="TAlgorithm" /> instance throws <see cref="ObjectDisposedException" /> whose
        /// <see cref="ObjectDisposedException.ObjectName" /> carries the concrete algorithm type
        /// name. Regression guard for defects where <c>nameof(T)</c> on a non-generic base class
        /// produced the literal string <c>"T"</c> instead of the derived type name.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenDisposes_ShouldReportConcreteTypeName()
        {
            var algorithm = CreateAlgorithm();
            algorithm.Dispose();

            try
            {
                using var _ = algorithm.CreateEncryptor();
                Assert.Fail("Expected ObjectDisposedException after disposal.");
            }
            catch (ObjectDisposedException ex)
            {
                Assert.AreEqual(typeof(TAlgorithm).FullName, ex.ObjectName,
                    $"ObjectDisposedException.ObjectName must match the concrete type name '{typeof(TAlgorithm).FullName}'.");
            }
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when the key is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenKeyIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = algorithm.CreateEncryptor(null!, new byte[algorithm.BlockSize / 8]);
            });
        }

        /// <summary>
        /// Verifies that <see cref="SymmetricAlgorithm.CreateEncryptor(byte[], byte[])" /> throws
        /// <see cref="ArgumentNullException" /> when the IV is <see langword="null" />.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenIvIsNull_ShouldThrowArgumentNullException_fix()
        {
            using var algorithm = CreateAlgorithm();

            Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = algorithm.CreateEncryptor(new byte[algorithm.KeySize / 8], null!);
            });
        }
    }
}