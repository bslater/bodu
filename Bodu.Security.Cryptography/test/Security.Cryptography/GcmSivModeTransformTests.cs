// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmSivModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class GcmSivModeTransformTests
        : AeadBlockCipherModeTests<GcmSivModeTransform>
    {
        protected override GcmSivModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new GcmSivModeTransform(cipher, k => new AesBlockCipherFixture(k), iv);
    }
}