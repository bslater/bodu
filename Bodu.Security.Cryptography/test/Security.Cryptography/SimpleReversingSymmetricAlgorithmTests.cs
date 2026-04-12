using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    [TestClass]
    public partial class SimpleReversingSymmetricAlgorithmTests
        : SymmetricAlgorithmTests<SimpleReversingSymmetricAlgorithm>
    {
        protected override SimpleReversingSymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
    }
}