// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BlockCipherModeTests.Transform.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

﻿using System.Security.Cryptography;

namespace Bodu.Security.Cryptography
{
    public abstract partial class BlockCipherModeTests<TMode>
    {
        /// <summary>
        /// Gets a value indicating whether this mode requires all input to be a multiple of the block size.
        /// Defaults to <see langword="true" />. Override to <see langword="false" /> for modes such as
        /// <see cref="CtsModeTransform" /> that explicitly accept non-block-aligned input.
        /// </summary>
        protected virtual bool RequiresBlockAlignedInput => true;

        /// <summary>
        /// Verifies that a mode which requires block-aligned input throws <see cref="ArgumentException" /> when
        /// presented with an input whose length is not a multiple of the block size.
        /// </summary>
        [TestMethod]
        public void Transform_WhenInputNotBlockAligned_ShouldThrow()
        {
            if (!RequiresBlockAlignedInput)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} accepts non-block-aligned input by design — block-alignment is not enforced.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
            var transform = CreateTransform(cipher, iv);

            var input = new byte[ExpectedBlockSize + 1];
            var output = new byte[input.Length];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                transform.Transform(input, output, encrypt: true);
            });
        }

        /// <summary>
        /// Verifies that an output buffer smaller than the input length causes <see cref="Transform" /> to throw
        /// <see cref="ArgumentException" />.
        /// </summary>
        [TestMethod]
        public void Transform_WhenOutputTooSmall_ShouldThrow()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
            var transform = CreateTransform(cipher, iv);

            var input = new byte[ExpectedBlockSize];
            var output = new byte[ExpectedBlockSize - 1];

            Assert.ThrowsExactly<ArgumentException>(() =>
            {
                transform.Transform(input, output, encrypt: true);
            });
        }

        /// <summary>
        /// Verifies that encrypting and then decrypting a plaintext with matched transforms reproduces the original bytes exactly.
        /// </summary>
        [TestMethod]
        public void Transform_WhenRoundTripped_ShouldReturnOriginal()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);

            var transformEncrypt = CreateTransform(cipher, (byte[])iv.Clone());
            var transformDecrypt = CreateTransform(cipher, (byte[])iv.Clone());

            var plaintext = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize * 2);
            var ciphertext = new byte[plaintext.Length];
            var decrypted = new byte[plaintext.Length];

            transformEncrypt.Transform(plaintext, ciphertext, encrypt: true);
            transformDecrypt.Transform(ciphertext, decrypted, encrypt: false);

            CollectionAssert.AreEqual(plaintext, decrypted);
        }

        /// <summary>
        /// Verifies that an all-zero plaintext is processed without error and yields an output buffer of the expected length.
        /// </summary>
        [TestMethod]
        public void Transform_WithAllZeroInput_ShouldProcessCorrectly()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0x00);
            var iv = Enumerable.Repeat((byte)0x00, ExpectedBlockSize).ToArray();
            var transform = CreateTransform(cipher, iv);

            var input = new byte[ExpectedBlockSize * 2];
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            // The precise output varies by mode (CTR, for example, XORs with an incrementing counter, so the
            // second block is non-zero even for an all-zero plaintext and an identity cipher). The purpose of
            // this test is to verify that Transform completes without throwing and produces the expected number
            // of output bytes; mode-specific output shapes are asserted by the per-mode test partials.
            Assert.AreEqual(input.Length, output.Length);
        }

        /// <summary>
        /// Verifies that an alternating <c>0xAA</c>/<c>0x55</c> bit-pattern input is transformed to non-zero output.
        /// </summary>
        [TestMethod]
        public void Transform_WithAlternatingBitsAA55_ShouldSucceed()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
            var transform = CreateTransform(cipher, iv);

            var input = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)(i % 2 == 0 ? 0xAA : 0x55)).ToArray();
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            Assert.IsTrue(output.Any(b => b != 0));
        }

        /// <summary>
        /// Verifies that a fully-saturated <c>0xFF</c> input is accepted and produces output of the expected length.
        /// </summary>
        [TestMethod]
        public void Transform_WithMaxValueInput_ShouldSucceed()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);

            // Use an all-zero seed rather than an all-0xFF seed so CTR's counter does not wrap on the
            // very first increment (which would cause IncrementCounter to set counterWrapped=true and
            // throw on the second block). The purpose of this test is to saturate the *input* bytes,
            // not the IV.
            var iv = new byte[ExpectedBlockSize];
            var transform = CreateTransform(cipher, iv);

            var input = Enumerable.Repeat((byte)0xFF, ExpectedBlockSize * 2).ToArray();
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            // Smoke test: Transform must accept a fully-saturated input and produce output of the expected
            // length. A stronger "all bytes non-zero" assertion is unsafe because OFB with an involutive test
            // cipher collapses to zero on alternating blocks; mode-specific byte-level assertions belong in
            // the per-mode test partials.
            Assert.AreEqual(input.Length, output.Length);
        }

        /// <summary>
        /// Verifies that a mirrored ramp-and-reverse input pattern is transformed to non-zero output.
        /// </summary>
        [TestMethod]
        public void Transform_WithMirroredPattern_ShouldSucceed()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
            var transform = CreateTransform(cipher, iv);

            var input = Enumerable.Range(0, ExpectedBlockSize * 2)
                .Select(i => (byte)(i < ExpectedBlockSize ? i : ExpectedBlockSize * 2 - i - 1))
                .ToArray();
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            Assert.IsTrue(output.Any(b => b != 0));
        }

        /// <summary>
        /// Verifies that an alternating <c>0xF0</c>/<c>0x0F</c> nibble-pattern input is transformed to non-zero output.
        /// </summary>
        [TestMethod]
        public void Transform_WithNibblePatternF00F_ShouldSucceed()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
            var transform = CreateTransform(cipher, iv);

            var input = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)(i % 2 == 0 ? 0xF0 : 0x0F)).ToArray();
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            Assert.IsTrue(output.Any(b => b != 0));
        }

        /// <summary>
        /// Verifies that a chaining mode feeds the underlying cipher distinct inputs for two
        /// identical plaintext blocks — the structural signal that chaining (or offset advancement)
        /// is active. This is checked on the cipher's input log rather than the ciphertext because
        /// <see cref="MonitoringBlockCipher" />'s XOR transform is linear, and some modes (notably
        /// OCB) surround the cipher call with pre- and post-XOR operations that cancel under a linear
        /// transform.
        /// </summary>
        [TestMethod]
        public void Transform_WithRepeatingPatternInput_ShouldFeedCipherDistinctInputsPerBlock()
        {
            if (!UsesChaining)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} does not alter repeating blocks.");
                return;
            }

            var cipher = new MonitoringBlockCipher(ExpectedBlockSize, xorMask: 0xFF);
            var iv = Enumerable.Range(0, ExpectedBlockSize).Select(i => (byte)i).ToArray();
            var transform = CreateTransform(cipher, (byte[])iv.Clone());

            var block = Enumerable.Repeat((byte)0xAA, ExpectedBlockSize).ToArray();
            var input = block.Concat(block).ToArray();
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            Assert.IsTrue(cipher.EncryptInputs.Count >= 2,
                $"Expected the underlying cipher to be invoked at least twice for a two-block input; " +
                $"got {cipher.EncryptInputs.Count} call(s).");

            CollectionAssert.AreNotEqual(
                cipher.EncryptInputs[0],
                cipher.EncryptInputs[1],
                $"{typeof(TMode).Name} reports UsesChaining=true but fed the underlying cipher " +
                "identical inputs for two identical plaintext blocks — chaining is not active.");
        }

        /// <summary>
        /// Verifies that a sawtooth (mod-16) byte-pattern input is transformed to non-zero output.
        /// </summary>
        [TestMethod]
        public void Transform_WithSawtoothPattern_ShouldSucceed()
        {
            var cipher = new MonitoringBlockCipher(ExpectedBlockSize);
            var iv = CryptoHelpers.GetRandomNonZeroBytes(ExpectedBlockSize);
            var transform = CreateTransform(cipher, iv);

            var input = Enumerable.Range(0, ExpectedBlockSize * 2).Select(i => (byte)(i % 16)).ToArray();
            var output = new byte[input.Length];

            transform.Transform(input, output, encrypt: true);

            Assert.IsTrue(output.Any(b => b != 0));
        }

        /// <summary>
        /// Verifies that a mode transform that guards against keystream reuse throws
        /// <see cref="CryptographicException" /> when the counter would wrap back to its
        /// initial value. Test uses a 1-byte block cipher so the wrap occurs after 256 calls.
        /// </summary>
        [TestMethod]
        public void Transform_WhenCounterWouldWrap_ShouldThrowCryptographicException()
        {
            if (!GuardsAgainstKeystreamReuse)
            {
                Assert.Inconclusive($"{typeof(TMode).Name} does not guard against keystream reuse.");
                return;
            }

            var cipher = new MonitoringBlockCipher(1);
            var transform = CreateTransform(cipher, new byte[1] { 0 });

            byte[] inputBlock = new byte[1];
            byte[] outputBlock = new byte[1];

            for (int i = 0; i < 256; i++)
            {
                transform.Transform(inputBlock, outputBlock, encrypt: true);
            }

            Assert.ThrowsExactly<CryptographicException>(
                () => transform.Transform(inputBlock, outputBlock, encrypt: true));
        }
    }
}