namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class SkipjackAlgorithmTests : SymmetricAlgorithmTests<Skipjack>
    {
        protected override Skipjack CreateAlgorithm() => new Skipjack();
    }
}
