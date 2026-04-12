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
        [TestMethod]
        public void BlockSize_WhenAccessed_ShouldReturnExpected()
        {
            var engine = CreateBlockCipher();
            var actual = engine.BlockSize;

            Assert.AreEqual(ExpectedBlockSize, actual);
        }
    }
}