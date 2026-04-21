// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class EaxModeTransformTests
        : BlockCipherModeTests<EaxModeTransform>
    {
        /// <inheritdoc />
        protected override EaxModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new EaxModeTransform(cipher, iv);

        /// <inheritdoc />
        /// <remarks>
        /// EAX uses CTR mode internally as its encryption primitive. The final CTR keystream block is
        /// truncated to the plaintext length, so non-block-aligned input is valid and must not throw.
        /// </remarks>
        protected override bool RequiresBlockAlignedInput => false;
    }
}