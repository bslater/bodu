// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtsModeTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class CtsModeTransformTests
    : BlockCipherModeTests<CtsModeTransform>
{
    protected override CtsModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new(cipher, iv);

    /// <summary>
    /// CTS explicitly accepts non-block-aligned input — that is its primary purpose. The base-class
    /// <c>Transform_WhenInputNotBlockAligned_ShouldThrow</c> test is suppressed for this mode.
    /// </summary>
    protected override bool RequiresBlockAlignedInput => false;

    /// <summary>
    /// CTS explicitly rejects sub-block input — it requires at least one full block by design.
    /// The empty-input no-op contract that <see cref="CbcModeTransform" /> honours therefore does
    /// not apply here.
    /// </summary>
    protected override bool AcceptsEmptyInput => false;
}
