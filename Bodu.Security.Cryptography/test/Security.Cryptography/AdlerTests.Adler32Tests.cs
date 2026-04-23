// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.Adler32Tests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Adler" /> hash algorithm.
/// </summary>
[TestClass]
public abstract partial class Adler32BaseTests<TTest, TAlgorithm>
    : AdlerTests<TTest, TAlgorithm, SingleTestVariant, uint>
    where TTest : Adler32BaseTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Adler32Base, new()
{
    public override IEnumerable<SingleTestVariant> GetHashAlgorithmVariants() => new[]
    {
        SingleTestVariant.Default
    };

    /// <inheritdoc />
    protected override HashAlgorithmSpecification GetSpecification(SingleTestVariant variant) =>
        new HashAlgorithmSpecification
        {
            HashSize = 32,
            InputBlockSize = 1,
            OutputBlockSize = 1,
        };

}
