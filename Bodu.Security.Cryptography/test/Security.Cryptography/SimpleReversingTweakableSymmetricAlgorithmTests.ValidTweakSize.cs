using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public partial class SimpleReversingTweakableSymmetricAlgorithmTests
    {
        // <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.ValidTweakSize(int)"/> returns <c>false</c> for values that are not
        /// considered valid tweak sizes. </summary> <param name="length">An invalid tweak size in bits.</param>
        [TestMethod]
        [DataRow(int.MinValue)]
        [DataRow(int.MaxValue)]
        [DataRow(0)]
        [DataRow(128)]
        [DataRow(160)]
        [DataRow(300)]
        [DataRow(384)]
        [DataRow(512)]
        [DataRow(4096)]
        public void ValidTweakSize_WhenLengthIsInvalid_ShouldReturnFalse(int length)
        {
            using var algorithm = this.CreateAlgorithm();
            bool result = algorithm.ValidTweakSize(length);
            Assert.IsFalse(result);
        }

        /// <summary>
        /// Verifies that <see cref="TweakableSymmetricAlgorithm.ValidTweakSize(int)" /> returns <c>true</c> for values that are expected to
        /// be valid tweak sizes.
        /// </summary>
        /// <param name="length">A valid tweak size in bits.</param>
        [TestMethod]
        [DataRow(64)]
        [DataRow(192)]
        [DataRow(256)]
        [DataRow(448)]
        [DataRow(576)]
        [DataRow(1024)]
        [DataRow(1536)]
        [DataRow(2048)]
        public void ValidTweakSize_WhenLengthIsValid_ShouldReturnTrue(int length)
        {
            using var algorithm = this.CreateAlgorithm();
            bool result = algorithm.ValidTweakSize(length);
            Assert.IsTrue(result);
        }
    }
}