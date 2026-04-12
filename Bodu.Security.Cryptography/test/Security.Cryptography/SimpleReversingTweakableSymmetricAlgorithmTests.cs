using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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