using System.Security.Cryptography;

namespace Bodu.Security.Cryptography.Extensions
{
    [TestClass]
    public partial class ICryptoTransformExtensionsTests
    {
        private SymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
    }
}