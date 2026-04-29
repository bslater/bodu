// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentCipherTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Serves as the abstract base test class for the Serpent cipher family, exposing the shared
/// <see cref="TweakableBlockCipherVariant" /> enumeration and a helper to create a fully initialised algorithm.
/// </summary>
/// <typeparam name="TTest">The concrete test class.</typeparam>
/// <typeparam name="TCipher">The concrete <see cref="SerpentBlockCipherBase" /> engine under test.</typeparam>
internal abstract partial class SerpentCipherTests<TTest, TCipher>
    : BlockCipherTests<TTest, TCipher, TweakableBlockCipherVariant>
    where TTest : SerpentCipherTests<TTest, TCipher>, new()
    where TCipher : SerpentBlockCipherBase
{
    /// <inheritdoc />
    public override IEnumerable<TweakableBlockCipherVariant> GetBlockCipherVariants() =>
        Enum.GetValues<TweakableBlockCipherVariant>().ToArray();

    /// <summary>
    /// Creates an initialised instance of the Serpent algorithm under test, with any required key, IV, and tweak
    /// material already generated.
    /// </summary>
    /// <returns>A fully initialised <see cref="SymmetricAlgorithm" /> ready for use.</returns>
    protected abstract SymmetricAlgorithm CreateInitialisedAlgorithm();
}
