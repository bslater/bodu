using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public partial class DelegateHashAlgorithmFactoryTests
    {
        /// <summary>
        /// Verifies that <see cref="DelegateHashAlgorithmFactory{T}.Create" /> invokes the supplied builder delegate and returns a
        /// fresh, non-null instance on each call.
        /// </summary>
        [TestMethod]
        public void Create_WhenBuilderProvided_ShouldReturnInstanceFromDelegate()
        {
            var factory = new DelegateHashAlgorithmFactory<MD5>(MD5.Create);

            using var first = factory.Create();
            using var second = factory.Create();

            Assert.IsNotNull(first);
            Assert.IsNotNull(second);
            Assert.AreNotSame(first, second);
        }
    }
}
