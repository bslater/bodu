using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public partial class TigerTests
    {
        [TestMethod]
        public void ComputeHash_WhenVariantIsDifferent_ShouldProduceDifferentHash()
        {
            var variants = Enum.GetValues<Bodu.Security.Cryptography.TigerHashingVariant>().ToArray();
            if (variants.Length < 2)
                Assert.Inconclusive("Not enough variants to test.");

            byte[] input = new byte[0];
            var actual = new List<byte[]>();
            foreach (var variant in variants)
            {
                using var algorithm = this.CreateAlgorithm();
                algorithm.Variant = variant;

                actual.Add(algorithm.ComputeHash(input));
            }

            CollectionAssert.AllItemsAreUnique(actual, "Hash results should be unique for different variants.");
        }
    }
}