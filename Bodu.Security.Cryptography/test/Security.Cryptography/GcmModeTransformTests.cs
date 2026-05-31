// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

[TestClass]
public sealed partial class GcmModeTransformTests
    : AeadBlockCipherModeTests<GcmModeTransformTests, GcmModeTransform>
{

    private const int NonceSizeBytes = 12;

    /// <inheritdoc />
    protected override string IvParameterName => "nonce";

    /// <inheritdoc />
    protected override int ExpectedInitializationVectorSize => NonceSizeBytes;

    protected override GcmModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new(cipher, iv);
}
