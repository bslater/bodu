using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public sealed partial class SkipjackAlgorithmTests : SymmetricAlgorithmTests<Skipjack>
    {
        protected override Skipjack CreateAlgorithm() => new Skipjack();
    }
}
