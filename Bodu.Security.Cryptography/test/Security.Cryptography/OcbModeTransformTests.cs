namespace Bodu.Security.Cryptography
{
    using System;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    /// <summary>
    /// Tests for <see cref="OcbModeTransform" /> (RFC 7253 — OCB3 AES-128, 128-bit tag).
    /// </summary>
    [TestClass]
    public sealed partial class OcbModeTransformTests : AeadBlockCipherModeTests<OcbModeTransform>
    {
        protected override int ExpectedBlockSize => 16;

        protected override OcbModeTransform CreateTransform(IBlockCipher cipher, byte[] iv)
            => new OcbModeTransform(cipher, iv);
    }
}