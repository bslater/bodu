namespace Bodu.Security.Cryptography
{
    public partial class CryptoHelpersTests
    {
        /// <summary>
        /// Verifies that GetRandomNonZeroBytes throws ArgumentOutOfRangeException when length is 0.
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void GetRandomNonZeroBytes_WhenLengthIsLessThanOrEqualToZero_ShouldThrowExactly()
        {
            _ = CryptoHelpers.GetRandomNonZeroBytes(0);
        }

        /// <summary>
        /// Verifies that GetRandomNonZeroBytes returns a buffer with only non-zero bytes.
        /// </summary>
        [TestMethod]
        public void GetRandomNonZeroBytes_WhenValidLength_ShouldReturnArrayWithOnlyNonZeroBytes()
        {
            byte[] result = CryptoHelpers.GetRandomNonZeroBytes(32);
            Assert.AreEqual(32, result.Length);
            CollectionAssert.DoesNotContain(result, (byte)0);
        }
    }
}