// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SivModeTransformTests.cs" company="PlaceholderCompany">
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class SivModeTransformTests
        : BlockCipherModeTests<SivModeTransform>
    {
        protected override SivModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new SivModeTransform(cipher, iv);
    }
}