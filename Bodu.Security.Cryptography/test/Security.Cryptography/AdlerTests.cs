// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AdlerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Numerics;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Contains unit tests for the <see cref="Adler" /> hash algorithm.
/// </summary>
[TestClass]
public abstract partial class AdlerTests<TTest, TAlgorithm, TVariant, TModulo>
    : HashAlgorithmTests<TTest, TAlgorithm, TVariant>
    where TTest : HashAlgorithmTests<TTest, TAlgorithm, TVariant>, new()
    where TAlgorithm : Adler<TModulo>, new()
    where TVariant : struct, Enum
    where TModulo : unmanaged, INumber<TModulo>
{
}
