// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OcbModeTransformTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

using Microsoft.VisualStudio.TestTools.UnitTesting;

/// <summary>
/// Tests for <see cref="OcbModeTransform" /> (RFC 7253 — OCB3 AES-128, 128-bit tag).
/// </summary>
[TestClass]
public sealed partial class OcbModeTransformTests
    : AeadBlockCipherModeTests<OcbModeTransformTests, OcbModeTransform>
{
    protected override int ExpectedBlockSize => 16;

    protected override OcbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
        => new(cipher, iv);

    // ── tagSize constructor overload ───────────────────────────────────────────────────────────
    // Supplements the base-class CreateTransform(cipher, iv) without replacing it so that
    // all inherited tests continue to exercise the default 128-bit tag path unchanged.
    private static OcbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv, int tagSize)
        => new(cipher, iv, tagSize);

}