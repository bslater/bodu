// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishAlgorithmTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Provides a common abstract test suite exercised against all <see cref="Threefish" /> variants
/// (Threefish-256, -512, -1024). Tests here cover contracts that are identical across all
/// block-size variants: tweak-related properties, <see cref="Threefish.GenerateTweak" />,
/// <see cref="TweakableSymmetricAlgorithm.LegalTweakSizes" />, the <see cref="Threefish.BlockMode" />
/// property, and the 3-arg overloads' tweak-length validation.
/// </summary>
[TestClass]
public abstract partial class ThreefishAlgorithmTests<TTest, TAlgorithm>
    : TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : ThreefishAlgorithmTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Threefish
{
}
