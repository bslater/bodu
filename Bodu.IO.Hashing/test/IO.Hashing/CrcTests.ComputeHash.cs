// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CrcTests.ComputeHash.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;

namespace Bodu.IO.Hashing;

public partial class CrcTests
{
    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> on the reference input
    /// <c>"123456789"</c> under CRC-32/ISO-HDLC produces the documented check value <c>0xCBF43926</c>, packed
    /// little-endian.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenInputIsReferenceString_ForCRC32_ISOHDLC_ShouldMatchPublishedCheck()
    {
        Crc crc = new(CrcStandard.CRC32_ISOHDLC);
        byte[] hash = crc.ComputeHash(RevEngCheckInput);

        CollectionAssert.AreEqual(new byte[] { 0x26, 0x39, 0xF4, 0xCB }, hash);
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
        byte[] second = crc.ComputeHash(RevEngCheckInput);

        byte[] reference = new Crc(CrcStandard.CRC32_ISOHDLC).ComputeHash(RevEngCheckInput);
        CollectionAssert.AreEqual(reference, second);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> produces the same digest as the
    /// streaming <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.Append(System.ReadOnlySpan{byte})" />
    /// + <see cref="System.IO.Hashing.NonCryptographicHashAlgorithm.GetCurrentHash()" /> pair across every
    /// representative CRC variant.
    /// </summary>
    /// <param name="variant">The CRC variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void ComputeHash_WhenComparedToStreamingHash_ShouldAgree(CrcTestVariant variant)
    {
        Crc oneShot = CreateAlgorithm(variant);
        Crc streaming = CreateAlgorithm(variant);
        streaming.Append(RevEngCheckInput);

        byte[] oneShotHash = oneShot.ComputeHash(RevEngCheckInput);
        byte[] streamingHash = streaming.GetCurrentHash();

        CollectionAssert.AreEqual(streamingHash, oneShotHash);
    }

    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> on an empty input produces the
    /// empty-input digest declared by the specification for each representative CRC variant.
    /// </summary>
    /// <param name="variant">The CRC variant under test.</param>
    [TestMethod]
    [DynamicData(nameof(NonCryptographicHashAlgorithmVariants))]
    public void ComputeHash_WhenInputIsEmpty_ShouldMatchSpecificationEmptyDigest(CrcTestVariant variant)
    {
        NonCryptographicHashAlgorithmSpecification specification = GetSpecification(variant);
        byte[] expected = System.Convert.FromHexString(specification.KnownAnswers.Empty!);

        Crc crc = CreateAlgorithm(variant);
        byte[] actual = crc.ComputeHash(System.Array.Empty<byte>());

        CollectionAssert.AreEqual(expected, actual);
    }
}
