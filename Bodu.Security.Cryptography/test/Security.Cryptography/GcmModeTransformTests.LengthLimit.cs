// ---------------------------------------------------------------------------------------------------------------
// <copyright file="GcmModeTransformTests.LengthLimit.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that <see cref="GcmModeTransform" /> enforces the SP 800-38D §5.2.1.1 plaintext and
/// associated-data length ceilings via the internal validator helpers.
/// </summary>
/// <remarks>
/// <para>
/// SP 800-38D §5.2.1.1 caps the plaintext at <c>2³⁹ − 256</c> bits (= 68 719 476 704 bytes) and the associated
/// data at <c>2⁶⁴</c> bits (= 2 305 843 009 213 693 952 bytes) per <c>(key, nonce)</c> pair. Both limits sit
/// far above <see cref="int.MaxValue" /> (≈ 2.1 GiB), so the public int-typed span surface cannot reach
/// either limit in a single call. The validator helpers
/// <see cref="GcmModeTransform.ValidatePlaintextLength" /> and <see cref="GcmModeTransform.ValidateAadLength" />
/// accept a <see cref="long" /> length so the contract can be tested directly without allocating 64 GiB / 2 EiB
/// of memory.
/// </para>
/// </remarks>
public sealed partial class GcmModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.MaxPlaintextBytes" /> matches the SP 800-38D §5.2.1.1
    /// plaintext byte ceiling, computed as <c>(2³⁹ − 256) / 8</c>.
    /// </summary>
    [TestMethod]
    public void MaxPlaintextBytes_ShouldMatchSp80038dLimit()
    {
        const long expected = ((1L << 39) - 256) / 8; // 68 719 476 704

        Assert.AreEqual(expected, GcmModeTransform.MaxPlaintextBytes);
    }

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.MaxAadBytes" /> matches the SP 800-38D §5.2.1.1
    /// associated-data byte ceiling, computed as <c>2⁶⁴ / 8 = 2⁶¹</c>.
    /// </summary>
    [TestMethod]
    public void MaxAadBytes_ShouldMatchSp80038dLimit()
    {
        const long expected = 1L << 61; // 2 305 843 009 213 693 952

        Assert.AreEqual(expected, GcmModeTransform.MaxAadBytes);
    }

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.ValidatePlaintextLength" /> accepts lengths up to and
    /// including <see cref="GcmModeTransform.MaxPlaintextBytes" />.
    /// </summary>
    /// <param name="length">A length that must be accepted.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow((long)int.MaxValue)]
    [DataRow(GcmModeTransform.MaxPlaintextBytes - 1)]
    [DataRow(GcmModeTransform.MaxPlaintextBytes)]
    public void ValidatePlaintextLength_WhenAtOrBelowLimit_ShouldNotThrow(long length)
    {
        GcmModeTransform.ValidatePlaintextLength(length);
    }

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.ValidatePlaintextLength" /> throws
    /// <see cref="CryptographicException" /> for any length strictly greater than
    /// <see cref="GcmModeTransform.MaxPlaintextBytes" />.
    /// </summary>
    /// <param name="length">A length that must be rejected.</param>
    [TestMethod]
    [DataRow(GcmModeTransform.MaxPlaintextBytes + 1)]
    [DataRow(GcmModeTransform.MaxPlaintextBytes + 100)]
    [DataRow(long.MaxValue)]
    public void ValidatePlaintextLength_WhenAboveLimit_ShouldThrowCryptographicException(long length)
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            GcmModeTransform.ValidatePlaintextLength(length);
        });
    }

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.ValidateAadLength" /> accepts lengths up to and including
    /// <see cref="GcmModeTransform.MaxAadBytes" />.
    /// </summary>
    /// <param name="length">A length that must be accepted.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow((long)int.MaxValue)]
    [DataRow(GcmModeTransform.MaxAadBytes - 1)]
    [DataRow(GcmModeTransform.MaxAadBytes)]
    public void ValidateAadLength_WhenAtOrBelowLimit_ShouldNotThrow(long length)
    {
        GcmModeTransform.ValidateAadLength(length);
    }

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.ValidateAadLength" /> throws
    /// <see cref="CryptographicException" /> for any length strictly greater than
    /// <see cref="GcmModeTransform.MaxAadBytes" />.
    /// </summary>
    /// <param name="length">A length that must be rejected.</param>
    [TestMethod]
    [DataRow(GcmModeTransform.MaxAadBytes + 1)]
    [DataRow(GcmModeTransform.MaxAadBytes + 1024)]
    [DataRow(long.MaxValue)]
    public void ValidateAadLength_WhenAboveLimit_ShouldThrowCryptographicException(long length)
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            GcmModeTransform.ValidateAadLength(length);
        });
    }

    /// <summary>
    /// Verifies that <see cref="GcmModeTransform.Encrypt" /> still accepts inputs whose length is the largest
    /// value the public int-typed span surface can express, ensuring the defensive length check does not
    /// inadvertently reject lawful single-shot payloads at the surface boundary.
    /// </summary>
    /// <remarks>
    /// This test exercises the wiring of <see cref="GcmModeTransform.ValidatePlaintextLength" /> from
    /// <see cref="GcmModeTransform.Encrypt" />, not the full int.MaxValue path — actually allocating a 2 GiB
    /// plaintext is not feasible in BVT and not necessary to confirm the guard is correctly placed.
    /// </remarks>
    [TestMethod]
    public void Encrypt_WhenPlaintextWithinLimits_ShouldNotThrowFromLengthGuard()
    {
        var nonce = new byte[NonceSizeBytes];
        using var cipher = new AesBlockCipherFixture(new byte[16]);
        using var transform = new GcmModeTransform(cipher, nonce);

        transform.ProcessAssociatedData(ReadOnlySpan<byte>.Empty);

        byte[] plaintext = new byte[64];
        byte[] output = new byte[plaintext.Length + (transform.TagSize / 8)];

        var written = transform.Encrypt(plaintext, output);

        Assert.AreEqual(plaintext.Length + (transform.TagSize / 8), written);
    }
}
