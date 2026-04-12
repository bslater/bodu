using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public abstract partial class FletcherTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that the constructor throws an <see cref="ArgumentException" /> when provided an invalid <c>hashSize</c>.
        /// </summary>
        /// <param name="hashSize">The invalid hash hashSize to test.</param>
        [TestMethod]
        [DataRow(0)]
        [DataRow(8)]
        [DataRow(63)]
        [DataRow(128)]
        [DataRow(-1)]
        public void Ctor_WhenHashSizeIsInvalid_ShouldThrowExactly(int hashSize)
        {
            Assert.ThrowsExactly<ArgumentException>(() => new TestFletcherHash(hashSize));
        }

        /// <summary>
        /// Verifies that the Fletcher constructor accepts only valid <c>hashSize</c> of 16, 32, or 64.
        /// </summary>
        [TestMethod]
        [DataRow(16)]
        [DataRow(32)]
        [DataRow(64)]
        public void Ctor_WhenHashSizeIsValid_ShouldSucceed(int hashSize)
        {
            var algorithm = new TestFletcherHash(hashSize);
            Assert.IsNotNull(algorithm);
        }

        private class TestFletcherHash
            : Security.Cryptography.Fletcher<TestFletcherHash>
        {
            public TestFletcherHash()
                : base(16)
            { }

            public TestFletcherHash(int hashSize)
                : base(hashSize)
            { }
        }
    }
}