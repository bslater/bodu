using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Security.Cryptography;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class CcmModeTransformTests
        : AeadBlockCipherModeTests<CcmModeTransform>
    {
        protected override CcmModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new CcmModeTransform(cipher, iv);
    }
}