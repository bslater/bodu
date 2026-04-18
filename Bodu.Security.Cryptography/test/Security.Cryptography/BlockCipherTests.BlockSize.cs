using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockCipherTests<TTest, TCipher, TVariant>
    {
        /// <summary>
        /// Verifies that <see cref="IBlockCipher.BlockSize" /> returns the value declared in the
        /// <see cref="BlockCipherSpecification" /> for the given variant.
        /// </summary>
        /// <param name="variant">The cipher variant that determines the expected block size.</param>
        [TestMethod]
        [DynamicData(nameof(BlockCipherVariants))]
        public void BlockSize_WhenAccessed_ShouldReturnExpected(TVariant variant)
        {
            var specification = this.GetSpecification(variant);
            using var engine = this.CreateBlockCipher(variant);

            Assert.AreEqual(
                specification.BlockSize,
                engine.BlockSize,
                $"[{variant}] Expected BlockSize of {specification.BlockSize} bytes but got {engine.BlockSize}.");
        }
    }
}