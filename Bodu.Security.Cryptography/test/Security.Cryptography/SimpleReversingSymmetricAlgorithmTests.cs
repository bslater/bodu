namespace Bodu.Security.Cryptography
{
    [TestClass]
    public partial class SimpleReversingSymmetricAlgorithmTests
        : SymmetricAlgorithmTests<SimpleReversingSymmetricAlgorithm>
    {
        protected override SimpleReversingSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
    }
}