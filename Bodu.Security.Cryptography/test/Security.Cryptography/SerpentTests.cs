// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SerpentTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared base for the wide-block tweakable Serpent algorithm-tier tests
/// (<see cref="Serpent256Tests" />, <see cref="Serpent512Tests" />, <see cref="Serpent1024Tests" />).
/// Inherits the full <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> contract surface and
/// hoists wiring that is identical across every Serpent block size — currently the
/// <see cref="Serpent.BlockMode" /> setter for <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}.SetBlockMode" />.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for
/// <see cref="DynamicDataAttribute" /> sources via the standard <c>new TTest()</c> dispatch idiom.</typeparam>
/// <typeparam name="TAlgorithm">The concrete <see cref="Serpent" />-derived algorithm under test.</typeparam>
public abstract partial class SerpentTests<TTest, TAlgorithm>
    : TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : SerpentTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Serpent
{
    /// <inheritdoc />
    protected sealed override void SetBlockMode(TAlgorithm algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;
}
