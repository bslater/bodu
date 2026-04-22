// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CipherModeTestsBase.Ctors.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    public abstract partial class CipherModeTestsBase<TTransform>
    {
        // ── Constructor validation tests ──────────────────────────────────────────────────────────

        /// <summary>
        /// Verifies that constructing the mode transform with a <see langword="null" /> IV throws
        /// <see cref="ArgumentNullException" /> whose <see cref="ArgumentException.ParamName" />
        /// equals <see cref="IvParameterName" />. Skipped for modes that do not accept an IV (ECB).
        /// </summary>
        [TestMethod]
        public void Ctor_WhenIvIsNull_ShouldThrowArgumentNullExceptionWithIvParamName()
        {
            if (!UsesInitializationVector)
            {
                Assert.Inconclusive($"{typeof(TTransform).Name} does not accept an initialisation vector.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);

            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
                _ = CreateTransform(cipher, null!));

            Assert.AreEqual(IvParameterName, ex.ParamName);
        }

        /// <summary>
        /// Verifies that constructing the mode transform with an IV whose length differs from the
        /// cipher block size throws <see cref="ArgumentException" /> whose
        /// <see cref="ArgumentException.ParamName" /> equals <see cref="IvParameterName" />.
        /// Skipped for modes that do not accept an IV (ECB).
        /// </summary>
        [TestMethod]
        public void Ctor_WhenIvLengthDoesNotMatchBlockSize_ShouldThrowArgumentException()
        {
            if (!UsesInitializationVector)
            {
                Assert.Inconclusive($"{typeof(TTransform).Name} does not accept an initialisation vector.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = new byte[ExpectedBlockSize - 1];

            var ex = Assert.ThrowsExactly<ArgumentException>(() =>
                _ = CreateTransform(cipher, iv));

            Assert.AreEqual(IvParameterName, ex.ParamName);
        }

        /// <summary>
        /// Verifies that constructing the mode transform with a valid cipher and a correctly-sized IV
        /// completes without throwing.
        /// </summary>
        [TestMethod]
        public void Ctor_WhenArgumentsAreValid_ShouldSucceed()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, iv);

            Assert.IsNotNull(transform);
        }
    }
}