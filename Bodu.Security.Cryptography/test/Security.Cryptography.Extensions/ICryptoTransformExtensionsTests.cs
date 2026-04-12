using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography.Extensions
{
    [TestClass]
    public partial class ICryptoTransformExtensionsTests
    {
        private SymmetricAlgorithm CreateAlgorithm() => new SimpleReversingSymmetricAlgorithm();
    }
}