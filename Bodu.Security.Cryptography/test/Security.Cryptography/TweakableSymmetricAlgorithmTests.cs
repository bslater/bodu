namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Provides unit tests for symmetric algorithms to verify encryption, decryption, and property behaviors.
    /// </summary>
    /// <typeparam name="TAlgorithm">The type of symmetric algorithm under test.</typeparam>
    [TestClass]
    public abstract partial class TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
        : SymmetricAlgorithmTests<TAlgorithm>
        where TTest : TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>, new()
        where TAlgorithm : Security.Cryptography.TweakableSymmetricAlgorithm
    {
    }
}