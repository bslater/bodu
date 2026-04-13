using System;

namespace Bodu.Security.Cryptography
{
    public partial class MerkleTreeHashTests
    {
        // -----------------------------------------------------------------------------------------
        // Disposal
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Verifies that calling Dispose twice does not throw.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenCalledTwice_ShouldNotThrow()
        {
            var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut);
            hasher.Dispose();
            hasher.Dispose();
        }

        /// <summary>
        /// Verifies that the instance can be used within a using statement without error.
        /// </summary>
        [TestMethod]
        public void Dispose_WhenUsedWithUsingStatement_ShouldDisposeCleanly()
        {
            byte[] result;
            using (var hasher = new MerkleTreeHash(Factory, DefaultBlockSize, DefaultFanOut))
                result = hasher.ComputeHash(MakeData(4));
            Assert.IsNotNull(result);
        }
    }
}