// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class GcmModeTransformTests
        : AeadBlockCipherModeTests<GcmModeTransform>
    {
        protected override GcmModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new GcmModeTransform(cipher, iv);
    }
}