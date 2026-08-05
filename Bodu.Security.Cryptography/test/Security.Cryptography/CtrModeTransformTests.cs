// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CtrModeTransformTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class CtrModeTransformTests
    : BlockCipherModeTests<CtrModeTransform>
{
    /// <inheritdoc />
    protected override CtrModeTransform CreateTransform(IBlockCipher cipher, byte[] iv) =>
        new(cipher, iv);

    /// <inheritdoc />
    /// <remarks>
    /// CTR's constructor parameter is named <c>initialCounter</c> rather than <c>iv</c>, so the
    /// argument-validation tests assert that exceptions expose this parameter name.
    /// </remarks>
    protected override string IvParameterName => "initialCounter";

    /// <inheritdoc />
    /// <remarks>
    /// CTR is a stream cipher construction: the last block of keystream is truncated to match the
    /// plaintext length, so non-block-aligned input is valid and must not throw.
    /// </remarks>
    protected override bool RequiresBlockAlignedInput => false;

    /// <inheritdoc />
    /// <remarks>
    /// CTR detects and refuses to reproduce the initial keystream after the internal counter wraps
    /// through its full value space, so
    /// <see cref="BlockCipherModeTests{TMode}.Transform_WhenCounterWouldWrap_ShouldThrowCryptographicException" />
    /// is executed for this mode.
    /// </remarks>
    protected override bool GuardsAgainstKeystreamReuse => true;
}
