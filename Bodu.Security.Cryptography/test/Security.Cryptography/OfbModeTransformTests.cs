using Microsoft.VisualStudio.TestTools.UnitTesting;
using Bodu.Security.Cryptography;
using Bodu.Testing.Security;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class OfbModeTransformTests
        : BlockCipherModeTests<OfbModeTransform>
    {
        /// <inheritdoc />
        protected override OfbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new OfbModeTransform(cipher, iv);
    }
}
