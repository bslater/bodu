using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public partial class DelegateHashAlgorithmFactoryTests
    {
        /// <summary>
        /// Verifies that constructing a <see cref="DelegateHashAlgorithmFactory{T}" /> with a <see langword="null" /> builder
        /// delegate throws <see cref="ArgumentNullException" /> with the expected parameter name.
        /// </summary>
        [TestMethod]
        public void Ctor_WhenBuilderIsNull_ShouldThrowArgumentNullException()
        {
            var ex = Assert.ThrowsExactly<ArgumentNullException>(() =>
            {
                _ = new DelegateHashAlgorithmFactory<MD5>((Func<MD5>)null!);
            });

            Assert.AreEqual("builder", ex.ParamName);
        }
    }
}
