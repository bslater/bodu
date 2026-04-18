// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EaxModeTransformTests.cs" company="PlaceholderCompany">
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class EaxModeTransformTests
        : BlockCipherModeTests<EaxModeTransform>
    {
        protected override EaxModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new EaxModeTransform(cipher, iv);
    }
}