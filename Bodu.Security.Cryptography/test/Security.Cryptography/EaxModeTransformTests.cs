// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class EaxModeTransformTests
        : AeadBlockCipherModeTests<EaxModeTransform>
    {
        protected override EaxModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new EaxModeTransform(cipher, iv);
    }
}
