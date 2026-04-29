// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreeFishCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

internal abstract partial class ThreeFishCipherTests<TTest, TCipher>
    : BlockCipherTests<TTest, TCipher, TweakableBlockCipherVariant>
    where TTest : ThreeFishCipherTests<TTest, TCipher>, new()
    where TCipher : ThreefishBlockCipher
{
    public override IEnumerable<TweakableBlockCipherVariant> GetBlockCipherVariants() =>
        Enum.GetValues<TweakableBlockCipherVariant>().ToArray();

    /// <summary>
    /// Creates an initialised instance of the Threefish variant under test with a freshly
    /// generated key, IV, and tweak.
    /// </summary>
    protected abstract Threefish CreateInitialisedAlgorithm();
}
