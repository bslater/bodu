// ---------------------------------------------------------------------------------------------------------------
// <copyright file="TigerTests.Nessie.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class TigerTests
{
    // ── NESSIE Project Tiger-192 known-answer tests ───────────────────────────────────────────
    //
    // Reference: Project NESSIE (New European Schemes for Signature, Integrity, and Encryption)
    // Tiger test-vector file, transcribed to NESSIE format at
    // github.com/ony/TigerHash/blob/master/test-vectors-nessie-format.dat. These are the standard
    // Tiger (0x01 padding) 192-bit digests. Set 1 pins the canonical string messages; the remaining
    // rows pin byte-aligned zero-length boundaries from Set 2.

    /// <summary>
    /// Verifies that <see cref="Tiger" /> in the standard (0x01-padding) 192-bit configuration reproduces
    /// the exact NESSIE Tiger reference digest for the given message.
    /// </summary>
    /// <param name="messageHex">The input message as a hex string (empty for the empty message).</param>
    /// <param name="expectedHex">The expected 192-bit Tiger digest as uppercase hex.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DataRow("", "3293AC630C13F0245F92BBB1766E16167A4E58492DDE73F3")]
    [DataRow("61", "77BEFBEF2E7EF8AB2EC8F93BF587A7FC613E247F5F247809")]
    [DataRow("616263", "2AAB1484E8C158F2BFB8C5FF41B57A525129131C957B5F93")]
    [DataRow("6D65737361676520646967657374", "D981F8CB78201A950DCF3048751E441C517FCA1AA55A29F6")]
    [DataRow("6162636465666768696A6B6C6D6E6F707172737475767778797A", "1714A472EEE57D30040412BFCC55032A0B11602FF37BEEE9")]
    [DataRow("6162636462636465636465666465666765666768666768696768696A68696A6B696A6B6C6A6B6C6D6B6C6D6E6C6D6E6F6D6E6F706E6F7071", "0F7BF9A19B9C58F2B7610DF7E84F0AC3A71C631E7B53F78E")]
    [DataRow("4142434445464748494A4B4C4D4E4F505152535455565758595A6162636465666768696A6B6C6D6E6F707172737475767778797A30313233343536373839", "8DCEA680A17583EE502BA38A3C368651890FFBCCDC49A8CC")]
    [DataRow("3132333435363738393031323334353637383930313233343536373839303132333435363738393031323334353637383930313233343536373839303132333435363738393031323334353637383930", "1C14795529FD9F207A958F84C52F11E887FA0CABDFD91BFD")]
    [DataRow("00", "5D9ED00A030E638BDB753A6A24FB900E5A63B8E73E6C25B6")]
    [DataRow("0000000000000000", "5229DC51A494B913F8E04C4C729C93CB8B260CA4EE8EA9D7")]
    [DataRow("00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000", "33FF9966DDD692427A9BC4D611F3C74CF629A0544A1A7ED7")]
    public void ComputeHash_WithNessieTigerVector_ShouldMatchReferenceDigest(string messageHex, string expectedHex)
    {
        byte[] message = Convert.FromHexString(messageHex);

        using var tiger = new Tiger(192) { Variant = TigerHashingVariant.Tiger };
        byte[] actual = tiger.ComputeHash(message);

        Assert.AreEqual(expectedHex, Convert.ToHexString(actual),
            $"Tiger-192 must match the NESSIE reference digest for a {message.Length}-byte message.");
    }
}
