using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Bodu.Security.Cryptography
{
    public enum SipHashVariant
    {
        SipHash_2_4,
        SipHash_4_8
    }

    public abstract partial class SipHashTests<TTest, TAlgorithm>
        : KeyedHashAlgorithmTests<TTest, TAlgorithm, SipHashVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, SipHashVariant>, new()
        where TAlgorithm : SipHash<TAlgorithm>, new()
    {
        protected override IEnumerable<string> GetFieldsToExcludeFromDisposeValidation()
        {
            var list = new List<string>(base.GetFieldsToExcludeFromDisposeValidation());
            list.AddRange([
                "BlockSizeBytes"
            ]);

            return list;
        }

        public override IEnumerable<SipHashVariant> GetHashAlgorithmVariants() => new[]
        {
            SipHashVariant.SipHash_2_4,
            SipHashVariant.SipHash_4_8
        };
    }
}