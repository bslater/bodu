// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.cs" company="PlaceholderCompany">
// </copyright>
// ---------------------------------------------------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class OcbModeTransformTests
        : BlockCipherModeTests<OcbModeTransform>
    {
        protected override OcbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new OcbModeTransform(cipher, iv);
    }
}