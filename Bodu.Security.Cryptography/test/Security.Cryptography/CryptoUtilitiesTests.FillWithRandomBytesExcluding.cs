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
        /// Verifies that FillWithRandomBytesExcluding fills a span without the forbidden byte.
        /// </summary>
        [TestMethod]
        public void FillWithRandomBytesExcluding_WhenForbiddenByteIsGiven_ShouldNotBeInResult()
        {
            Span<byte> span = stackalloc byte[64];
            CryptoHelpers.FillWithRandomBytesExcluding(0xFF, span);
            foreach (byte b in span)
            {
                Assert.AreNotEqual(0xFF, b);
            }
        }
    }
}