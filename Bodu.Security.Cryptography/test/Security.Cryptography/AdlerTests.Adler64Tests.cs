// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Adler" /> hash algorithm.
    /// </summary>
    [TestClass]
    public abstract partial class Adler64BaseTests<TTest, TAlgorithm>
        : AdlerTests<TTest, TAlgorithm, SingleTestVariant, ulong>
        where TTest : Adler64BaseTests<TTest, TAlgorithm>, new()
        where TAlgorithm : Adler64Base, new()
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };

        /// <inheritdoc />
        protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
            new HashAlgorithmSpecification
            {
                HashSize = 64,
                InputBlockSize = 1,
                OutputBlockSize = 1,
            };

    }
}