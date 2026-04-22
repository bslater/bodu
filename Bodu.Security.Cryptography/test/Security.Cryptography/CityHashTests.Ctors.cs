// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CityHashTests.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public abstract partial class CityHashTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that the base-class constructor throws an <see cref="ArgumentOutOfRangeException" />
        /// when provided an unsupported <c>hashSize</c> value.
        /// </summary>
        /// <param name="hashSize">The invalid hash size in bits.</param>
        [TestMethod]
        [DataRow(0)]
        [DataRow(8)]
        [DataRow(16)]
        [DataRow(31)]
        [DataRow(33)]
        [DataRow(63)]
        [DataRow(65)]
        [DataRow(127)]
        [DataRow(129)]
        [DataRow(256)]
        [DataRow(-1)]
        public void Ctor_WhenHashSizeIsInvalid_ShouldThrowExactly(int hashSize)
        {
            Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            {
                _ = new TestCityHash(hashSize);
            });
        }

        /// <summary>
        /// Verifies that the base-class constructor succeeds for each supported hash size: 32, 64, and 128 bits.
        /// </summary>
        /// <param name="hashSize">A valid hash size in bits.</param>
        [TestMethod]
        [DataRow(32)]
        [DataRow(64)]
        [DataRow(128)]
        public void Ctor_WhenHashSizeIsValid_ShouldSucceed(int hashSize)
        {
            using var algorithm = new TestCityHash(hashSize);
            Assert.IsNotNull(algorithm);
        }

        // ---------------------------------------------------------------------------------------------------------------
        // Inner helper — exposes the protected CityHashBase<T> constructor for parameter validation tests.
        // This mirrors the TestSipHash pattern in SipHashTests_Ctor.cs.
        // ---------------------------------------------------------------------------------------------------------------

        private sealed class TestCityHash : CityHash<TAlgorithm>
        {
            public TestCityHash(int hashSize) : base(hashSize) { }

            protected override byte[] ComputeHashCore(ReadOnlySpan<byte> source) =>
                Array.Empty<byte>();
        }
    }
}