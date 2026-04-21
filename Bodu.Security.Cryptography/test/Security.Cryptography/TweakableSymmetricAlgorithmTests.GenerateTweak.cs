namespace Bodu.Security.Cryptography
{
    public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    {
        /// <summary>
        /// Verifies that GenerateTweak creates a tweak of expected length.
        /// </summary>
        [TestMethod]
        public void GenerateTweak_WhenCalled_ShouldInitializeTweakCorrectly()
        {
            using var algorithm = CreateAlgorithm();
            int size = algorithm.LegalTweakSizes[0].MinSize;
            algorithm.TweakSize = size;
            algorithm.GenerateTweak();

            Assert.AreEqual(size / 8, algorithm.Tweak.Length);
        }
    }
}