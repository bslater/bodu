using Bodu.Infrastructure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bodu.Security.Cryptography
{
    public enum ThreeFishCipherTestVariant
    {
        ZeroedKeyAndTweak,
        DefaultKeyAndTweak
    }

    internal abstract partial class ThreeFishCipherTests<TTest, TCipher>
        : BlockCipherTests<TTest, TCipher, ThreeFishCipherTestVariant>
        where TTest : ThreeFishCipherTests<TTest, TCipher>, new()
        where TCipher : ThreefishBlockCipher
    {
        public override IEnumerable<ThreeFishCipherTestVariant> GetBlockCipherVariants() =>
            Enum.GetValues<ThreeFishCipherTestVariant>().ToArray();
    }
}