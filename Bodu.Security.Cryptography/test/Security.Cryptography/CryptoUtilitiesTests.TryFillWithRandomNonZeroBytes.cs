using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public partial class CryptoUtilitiesTests
    {
        /// <summary>
        /// Verifies that TryFillWithRandomNonZeroBytes returns true and fills span with non-zero bytes.
        /// </summary>
        [TestMethod]
        public void TryFillWithRandomNonZeroBytes_WhenBufferCanBeFilled_ShouldReturnTrue()
        {
            Span<byte> span = stackalloc byte[32];
            bool result = CryptoHelpers.TryFillWithRandomNonZeroBytes(span);
            Assert.IsTrue(result);
            foreach (byte b in span)
            {
                Assert.AreNotEqual(0, b);
            }
        }
    }
}