// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CcmModeTransformTests.LengthLimit.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Security.Cryptography;

/// <summary>
/// Verifies that <see cref="CcmModeTransform" /> enforces the message-length ceiling imposed by its 3-byte length
/// field (<c>q = 3</c>) via the internal validator helper.
/// </summary>
/// <remarks>
/// CCM encodes the message length in only three bytes of the B0 block and drives a 3-byte CTR counter, so the
/// message must be smaller than <c>2²⁴</c> bytes. A longer message would silently truncate the CBC-MAC length field
/// and wrap the counter, reusing keystream — a real authentication failure. The validator accepts a <see cref="long" />
/// so the boundary can be exercised without allocating a 16 MiB buffer.
/// </remarks>
public sealed partial class CcmModeTransformTests
{
    /// <summary>
    /// Verifies that <see cref="CcmModeTransform.MaxPlaintextBytes" /> equals the maximum value encodable in the
    /// 3-byte length field, <c>2²⁴ − 1</c>.
    /// </summary>
    [TestMethod]
    public void MaxPlaintextBytes_ShouldMatchThreeByteLengthFieldLimit()
    {
        const int expected = (1 << 24) - 1; // 16 777 215

        Assert.AreEqual(expected, CcmModeTransform.MaxPlaintextBytes);
    }

    /// <summary>
    /// Verifies that <see cref="CcmModeTransform.ValidatePlaintextLength" /> accepts lengths up to and including
    /// <see cref="CcmModeTransform.MaxPlaintextBytes" />.
    /// </summary>
    /// <param name="length">A length that must be accepted.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow((long)CcmModeTransform.MaxPlaintextBytes - 1)]
    [DataRow((long)CcmModeTransform.MaxPlaintextBytes)]
    public void ValidatePlaintextLength_WhenAtOrBelowLimit_ShouldNotThrow(long length) =>
        CcmModeTransform.ValidatePlaintextLength(length);

    /// <summary>
    /// Verifies that <see cref="CcmModeTransform.ValidatePlaintextLength" /> throws
    /// <see cref="CryptographicException" /> for any length strictly greater than
    /// <see cref="CcmModeTransform.MaxPlaintextBytes" />, so an over-long message cannot silently truncate the
    /// length field or wrap the counter.
    /// </summary>
    /// <param name="length">A length that must be rejected.</param>
    [TestMethod]
    [DataRow((long)CcmModeTransform.MaxPlaintextBytes + 1)]
    [DataRow((long)CcmModeTransform.MaxPlaintextBytes + 100)]
    [DataRow(long.MaxValue)]
    public void ValidatePlaintextLength_WhenAboveLimit_ShouldThrowCryptographicException(long length)
    {
        Assert.ThrowsExactly<CryptographicException>(() =>
        {
            CcmModeTransform.ValidatePlaintextLength(length);
        });
    }
}
