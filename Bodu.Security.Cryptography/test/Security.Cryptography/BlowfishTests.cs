namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class BlowfishTests
        : SymmetricAlgorithmTests<Blowfish>
    {
        protected override Blowfish CreateAlgorithm() => Blowfish.Create();
    }
}
