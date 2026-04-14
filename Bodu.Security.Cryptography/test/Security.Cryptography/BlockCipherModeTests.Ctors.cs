using Bodu.Testing.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockCipherModeTests<TMode>
    {
        /// <summary>
        /// Verifies that constructing the mode transform with a null IV either throws
        /// <see cref="ArgumentNullException" /> with <c>ParamName == "iv"</c> (for modes
        /// that validate) or succeeds (for modes like ECB that do not take an IV).
        /// </summary>
        [TestMethod]
        public void Ctor_WhenIvIsNull_ShouldThrowArgumentNullExceptionWithIvParamName()
        {
            if (!this.ValidatesIvLengthAtConstruction)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} does not validate IV length at construction.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(                () =>
            {
                _ = this.CreateTransform(cipher, null!);
            });

            Assert.AreEqual("iv", ex.ParamName);
        }

        /// <summary>
        /// Verifies that constructing the mode transform with an IV whose length differs from
        /// the cipher's block size throws <see cref="ArgumentException" /> with
        /// <c>ParamName == "iv"</c>.
        /// </summary>
        [TestMethod]
        public void Ctor_WhenIvLengthDoesNotMatchBlockSize_ShouldThrowArgumentException()
        {
            if (!this.ValidatesIvLengthAtConstruction)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} does not validate IV length at construction.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            byte[] iv = new byte[ExpectedBlockSize - 1];

            var ex = Assert.ThrowsExactly<ArgumentException>(                () =>
            {
                _ = this.CreateTransform(cipher, iv);
            });

            Assert.AreEqual("iv", ex.ParamName);
        }
    }
}
