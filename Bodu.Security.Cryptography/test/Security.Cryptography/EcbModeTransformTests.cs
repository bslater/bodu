using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Security.Cryptography;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class EcbModeTransformTests
        : BlockCipherModeTests<EcbModeTransform>
    {
        protected override EcbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new EcbModeTransform(cipher);
    }
}