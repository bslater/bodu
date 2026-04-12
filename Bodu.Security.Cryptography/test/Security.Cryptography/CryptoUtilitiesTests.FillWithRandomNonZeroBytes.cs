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
        /// Verifies that FillWithRandomNonZeroBytes throws ArgumentNullException when the buffer is null.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void FillWithRandomNonZeroBytes_WhenBufferIsNull_ShouldThrowExactly()
        {
            CryptoHelpers.FillWithRandomNonZeroBytes(null);
        }

        /// <summary>
        /// Verifies that FillWithRandomNonZeroBytes throws ArgumentException when the buffer is empty.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void FillWithRandomNonZeroBytes_WhenBufferIsEmpty_ShouldThrowExactly()
        {
            CryptoHelpers.FillWithRandomNonZeroBytes(Array.Empty<byte>());
        }

        /// <summary>
        /// Verifies that FillWithRandomNonZeroBytes fills the buffer with only non-zero values.
        /// </summary>
        [TestMethod]
        public void FillWithRandomNonZeroBytes_WhenBufferHasLength_ShouldContainNoZeroBytes()
        {
            byte[] buffer = new byte[32];
            CryptoHelpers.FillWithRandomNonZeroBytes(buffer);
            CollectionAssert.DoesNotContain(buffer, (byte)0);
        }

        /// <summary>
        /// Verifies that FillWithRandomNonZeroBytes fills a span with only non-zero bytes.
        /// </summary>
        [TestMethod]
        public void FillWithRandomNonZeroBytesSpan_WhenCalled_ShouldContainNoZeroBytes()
        {
            Span<byte> span = stackalloc byte[32];
            CryptoHelpers.FillWithRandomNonZeroBytes(span);
            foreach (byte b in span)
            {
                Assert.AreNotEqual(0, b);
            }
        }
    }
}