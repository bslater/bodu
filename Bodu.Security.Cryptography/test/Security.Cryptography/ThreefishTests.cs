// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ThreefishTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Shared base for the tweakable Threefish algorithm-tier tests
/// (<see cref="Threefish256Tests" />, <see cref="Threefish512Tests" />, <see cref="Threefish1024Tests" />).
/// Inherits the full <see cref="TweakableSymmetricAlgorithmTests{TTest, TAlgorithm}" /> contract surface and
/// hoists wiring that is identical across every Threefish block size — currently the
/// <see cref="Threefish.BlockMode" /> setter for <see cref="SymmetricAlgorithmTests{TTest, TAlgorithm}.SetBlockMode" />.
/// </summary>
/// <typeparam name="TTest">The concrete test class, used to resolve specification data for
/// <see cref="DynamicDataAttribute" /> sources via the standard <c>new TTest()</c> dispatch idiom.</typeparam>
/// <typeparam name="TAlgorithm">The concrete <see cref="Threefish" />-derived algorithm under test.</typeparam>
public abstract partial class ThreefishTests<TTest, TAlgorithm>
    : TweakableSymmetricAlgorithmTests<TTest, TAlgorithm>
    where TTest : ThreefishTests<TTest, TAlgorithm>, new()
    where TAlgorithm : Threefish, new()
{
    /// <inheritdoc />
    protected sealed override void SetBlockMode(TAlgorithm algorithm, CipherModeKind mode) =>
        algorithm.BlockMode = mode;

    /// <inheritdoc />
    protected sealed override CipherModeKind GetBlockMode(TAlgorithm algorithm) =>
        algorithm.BlockMode;
}
