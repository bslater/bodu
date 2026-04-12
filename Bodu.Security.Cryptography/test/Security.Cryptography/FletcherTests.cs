// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Fletcher" /> hash algorithm.
    /// </summary>
    [TestClass]
    public abstract partial class FletcherTests<TTest, TAlgorithm>
        : HashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
        where TTest : HashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>, new()
        where TAlgorithm : Fletcher<TAlgorithm>, new()
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        protected override IEnumerable<string> GetFieldsToExcludeFromDisposeValidation()
        {
            var list = new List<string>(base.GetFieldsToExcludeFromDisposeValidation());
            list.AddRange([
                "this.modulus",
                "BlockSizeBytes"
            ]);

            return list;
        }
    }
}