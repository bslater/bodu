namespace Bodu.Security.Cryptography
{
    [TestClass]
    public partial class SimpleReversingTweakableSymmetricAlgorithmTests
        : TweakableSymmetricAlgorithmTests<SimpleReversingTweakableSymmetricAlgorithmTests, SimpleReversingTweakableSymmetricAlgorithm>
    {
        /// <inheritdoc />
        protected override SimpleReversingTweakableSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingTweakableSymmetricAlgorithm();
    }
}