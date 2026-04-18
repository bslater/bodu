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
        : BlockHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>
        where TTest : BlockHashAlgorithmTests<TTest, TAlgorithm, SingleTestVariant>, new()
        where TAlgorithm : Fletcher<TAlgorithm>, new()
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };
    }
}