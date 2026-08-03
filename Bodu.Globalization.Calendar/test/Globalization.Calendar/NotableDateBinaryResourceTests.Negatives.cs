// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.Negatives.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>
    /// Rewrites the header's payload digest so a deliberately mutated payload still passes the integrity check and
    /// reaches structural validation.
    /// </summary>
    /// <param name="pack">The pack whose digest is recomputed in place.</param>
    private static void RehashPayload(byte[] pack) =>
        SHA256.HashData(pack.AsSpan(NotableDateBinaryFormat.HeaderLength), pack.AsSpan(8, SHA256.HashSizeInBytes));

    /// <summary>
    /// Verifies that input without the BCAL signature is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenMagicIsInvalid_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        pack[0] ^= 0xFF;

        _ = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });
    }

    /// <summary>
    /// Verifies that a pack declaring an unsupported format version is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenVersionIsUnsupported_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        pack[4] = 0xFF;
        pack[5] = 0xFF;

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.IsTrue(ex.Message.Contains("version", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that a corrupted payload fails the integrity digest before any content is interpreted.
    /// </summary>
    [TestMethod]
    public void Read_WhenPayloadCorrupted_ShouldThrowHashMismatch()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        pack[NotableDateBinaryFormat.HeaderLength + 3] ^= 0xFF;

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.IsTrue(ex.Message.Contains("digest", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that truncating the pack at every possible length is rejected with the format exception — never an
    /// out-of-range access or any other exception type.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenTruncatedAtEveryLength_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());

        for (var length = 0; length < pack.Length; length++)
        {
            byte[] truncated = pack.AsSpan(0, length).ToArray();
            if (length > NotableDateBinaryFormat.HeaderLength)
                RehashPayload(truncated);

            _ = Assert.ThrowsExactly<NotableDateBinaryFormatException>(
                () =>
                {
                    _ = ReadPack(truncated);
                },
                $"truncation at {length} bytes must fail cleanly");
        }
    }

    /// <summary>
    /// Verifies that mutating any single payload byte — with the digest recomputed so structural validation is
    /// reached — either decodes successfully or fails with the format exception; no other exception type may escape.
    /// The sweep covers undefined enum bytes, bad discriminators, out-of-range string references, and corrupted
    /// variable-length integers wherever they occur in the payload.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenAnyPayloadByteMutated_ShouldFailCleanlyOrDecode()
    {
        byte[] pack = WritePack(ComprehensiveResource());

        for (int offset = NotableDateBinaryFormat.HeaderLength; offset < pack.Length; offset++)
        {
            byte[] mutated = (byte[])pack.Clone();
            mutated[offset] ^= 0xFF;
            RehashPayload(mutated);

            try
            {
                _ = ReadPack(mutated);
            }
            catch (NotableDateBinaryFormatException)
            {
                // The sealed format rejected the mutation - the expected failure mode.
            }
            catch (Exception ex)
            {
                Assert.Fail($"Mutation at offset {offset} escaped as {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Verifies that trailing bytes after the resource body are rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenPayloadCarriesTrailingBytes_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        byte[] padded = new byte[pack.Length + 4];
        pack.CopyTo(padded, 0);
        RehashPayload(padded);

        _ = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(padded);
        });
    }
}
