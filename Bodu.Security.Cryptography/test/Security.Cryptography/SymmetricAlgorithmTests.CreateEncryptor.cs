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
        /// Validates that setting <see cref="SymmetricAlgorithm.CreateEncryptor" /> after the algorithm has been disposed throws
        /// an <see cref="ObjectDisposedException" />.
        /// </summary>
        [TestMethod]
        public void CreateEncryptor_WhenSetAfterDispose_ShouldThrowObjectDisposedException()
        {
            TAlgorithm algorithm = this.CreateAlgorithm();
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
            var algorithm = this.CreateAlgorithm();
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
    }
}