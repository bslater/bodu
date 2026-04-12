// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography
{
    /// <summary>
    /// Contains unit tests for the <see cref="Adler" /> hash algorithm.
    /// </summary>
    [TestClass]
    public abstract partial class Adler64BaseTests<TTest, TAlgorithm>
        : AdlerBaseTests<TTest, TAlgorithm, SingleTestVariant, ulong>
        where TTest : Adler64BaseTests<TTest, TAlgorithm>, new()
        where TAlgorithm : Adler64Base, new()
    {
        public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
        {
            SingleTestVariant.Default
        };
    }
}