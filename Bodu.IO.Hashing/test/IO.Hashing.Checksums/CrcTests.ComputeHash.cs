// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.ComputeHash.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Hashing.Checksums;

public partial class CrcTests
{

    /// <summary>
    /// Verifies that a large multi-word input hashed in a single call yields the same digest as feeding the same bytes
    /// one at a time. The one-shot path exercises any word-at-a-time (slicing) inner loop while the per-byte path
    /// exercises the byte-wise loop, so agreement pins the fast path to the reference for reflected 32/64-bit standards
    /// as well as the non-reflected standards that use the byte-wise fallback.
    /// </summary>
    /// <param name="standardName">The name of the <see cref="CrcStandard" /> under test.</param>
    [TestMethod]
    [DataRow(nameof(CrcStandard.CRC32_ISOHDLC))]
    [DataRow(nameof(CrcStandard.CRC32_ISCSI))]
    [DataRow(nameof(CrcStandard.CRC32_BZIP2))]
    [DataRow(nameof(CrcStandard.CRC64_XZ))]
    [DataRow(nameof(CrcStandard.CRC64_ECMA182))]
    public void ComputeHash_WhenLargeInput_ShouldMatchPerByteStreaming(string standardName)
    {
        CrcStandard standard = (CrcStandard)typeof(CrcStandard)
            .GetProperty(standardName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!
            .GetValue(null)!;

        byte[] input = new byte[1031]; // deliberately not a multiple of 8, to exercise the tail
        for (int i = 0; i < input.Length; i++)
            input[i] = (byte)((i * 37) + 11);

        byte[] oneShot = new Crc(standard).ComputeHash(input);

        var perByte = new Crc(standard);
        foreach (byte b in input)
            perByte.Append(new[] { b });
        byte[] streamed = perByte.GetCurrentHash();

        CollectionAssert.AreEqual(streamed, oneShot,
            $"One-shot and per-byte digests differ for {standardName}.");
    }

    /// <summary>
    /// Verifies that calling <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> twice on different inputs
    /// returns the digest of the second input — that is, the method resets internal state before hashing so that
    /// residual accumulator state from the prior call cannot bleed into the new computation.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenCalledTwice_ShouldResetBeforeHashing()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);

        _ = crc.ComputeHash(Encoding.ASCII.GetBytes("first input, different length"));
        byte[] second = crc.ComputeHash(s_revEngCheckInput);

        byte[] reference = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(s_revEngCheckInput);
        CollectionAssert.AreEqual(reference, second);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> resets internal state after
    /// finalization, so a subsequent <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" />
    /// hashes only the newly appended data rather than the concatenation with the prior one-shot input. This
    /// matches the reset-before-and-after semantics of the shared <c>ComputeHash</c> extension.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenFollowedByAppend_ShouldNotRetainPriorInput()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);

        _ = crc.ComputeHash(Encoding.ASCII.GetBytes("prior one-shot input"));
        crc.Append(s_revEngCheckInput);
        byte[] appended = crc.GetCurrentHash();

        byte[] reference = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(s_revEngCheckInput);
        CollectionAssert.AreEqual(reference, appended);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> produces the same digest as the
    /// streaming <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" />
    /// + <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> pair across every
    /// representative CRC variant.
    /// </summary>
    /// <param name="variant">The CRC variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void ComputeHash_WhenComparedToStreamingHash_ShouldAgree(CrcTestVariant variant)
    {
        Crc oneShot = CreateAlgorithm(variant);
        Crc streaming = CreateAlgorithm(variant);
        streaming.Append(s_revEngCheckInput);

        byte[] oneShotHash = oneShot.ComputeHash(s_revEngCheckInput);
        byte[] streamingHash = streaming.GetCurrentHash();

        CollectionAssert.AreEqual(streamingHash, oneShotHash);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> on an empty input produces the
    /// empty-input digest declared by the specification for each representative CRC variant.
    /// </summary>
    /// <param name="variant">The CRC variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants), DynamicDataDisplayName = nameof(NonCryptographicHashAlgorithmVariantDisplayName.GetDisplayName), DynamicDataDisplayNameDeclaringType = typeof(NonCryptographicHashAlgorithmVariantDisplayName))]
    public void ComputeHash_WhenInputIsEmpty_ShouldMatchSpecificationEmptyDigest(CrcTestVariant variant)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        byte[] expected = System.Convert.FromHexString(specification.KnownAnswers.Empty!);

        Crc crc = CreateAlgorithm(variant);
        byte[] actual = crc.ComputeHash([]);

        CollectionAssert.AreEqual(expected, actual);
    }
    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> on the reference input
    /// <c>"123456789"</c> under CRC-32/ISO-HDLC produces the documented check value <c>0xCBF43926</c>, packed
    /// little-endian.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsReferenceString_ForCRC32_ISOHDLC_ShouldMatchPublishedCheck()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] hash = crc.ComputeHash(s_revEngCheckInput);

        CollectionAssert.AreEqual(new byte[] { 0x26, 0x39, 0xF4, 0xCB }, hash);
    }

}
