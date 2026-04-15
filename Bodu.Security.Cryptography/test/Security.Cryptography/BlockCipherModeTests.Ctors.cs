using Bodu.Testing.Security;
using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockCipherModeTests<TMode>
    {
        /// <summary>
        /// Verifies that constructing the mode transform with a null initialisation vector throws
        /// <see cref="ArgumentNullException" /> whose <see cref="ArgumentException.ParamName" /> matches the mode's IV
        /// parameter name. Skipped for modes that do not accept an IV (for example, ECB).
        /// </summary>
        [TestMethod]
        public void Ctor_WhenIvIsNull_ShouldThrowArgumentNullExceptionWithIvParamName()
        {
            if (!this.UsesInitializationVector)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} does not accept an initialisation vector.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(
                () =>
                {
                    _ = this.CreateTransform(cipher, null!);
                });

            Assert.AreEqual(this.IvParameterName, ex.ParamName);
        }

        /// <summary>
        /// Verifies that constructing the mode transform with an initialisation vector whose length differs from the cipher's
        /// block size throws <see cref="ArgumentException" /> whose <see cref="ArgumentException.ParamName" /> matches the
        /// mode's IV parameter name. Skipped for modes that do not accept an IV (for example, ECB).
        /// </summary>
        [TestMethod]
        public void Ctor_WhenIvLengthDoesNotMatchBlockSize_ShouldThrowArgumentException()
        {
            if (!this.UsesInitializationVector)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} does not accept an initialisation vector.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            byte[] iv = new byte[ExpectedBlockSize - 1];

            var ex = Assert.ThrowsExactly<ArgumentException>(
                () =>
                {
                    _ = this.CreateTransform(cipher, iv);
                });

            Assert.AreEqual(this.IvParameterName, ex.ParamName);
        }

        /// <summary>
        /// Verifies that constructing the mode transform with a valid initialisation vector (when applicable) and a valid
        /// cipher completes without throwing.
        /// </summary>
        [TestMethod]
        public void Ctor_WhenArgumentsAreValid_ShouldSucceed()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            byte[] iv = new byte[ExpectedBlockSize];

            var transform = this.CreateTransform(cipher, iv);

            Assert.IsNotNull(transform);
        }
    }
}
