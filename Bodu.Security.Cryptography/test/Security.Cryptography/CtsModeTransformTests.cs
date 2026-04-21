namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class CtsModeTransformTests
        : BlockCipherModeTests<CtsModeTransform>
    {
        protected override CtsModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new CtsModeTransform(cipher, iv);

        /// <summary>
        /// CTS explicitly accepts non-block-aligned input — that is its primary purpose. The base-class
        /// <c>Transform_WhenInputNotBlockAligned_ShouldThrow</c> test is suppressed for this mode.
        /// </summary>
        protected override bool RequiresBlockAlignedInput => false;
    }
}