namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class CfbModeTransformTests
        : BlockCipherModeTests<CfbModeTransform>
    {
        /// <inheritdoc />
        protected override CfbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new CfbModeTransform(cipher, iv);
    }
}
