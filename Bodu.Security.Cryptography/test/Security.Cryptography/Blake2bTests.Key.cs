// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bTests.Key.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Blake2bTests
{
    /// <summary>
    /// Verifies that the keyed BLAKE2b-512 digest of an empty message with a 64-byte sequential key matches the
    /// known-answer value from the BLAKE2 reference test vectors (blake2b-kat.txt, first entry).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithEmptyInputAndFullKey_ShouldMatchKnownReferenceVector()
    {
        // Key: 64 sequential bytes 0x00..0x3f; input: empty.
        // Reference: https://github.com/BLAKE2/BLAKE2/blob/master/testvectors/blake2b-kat.txt
        byte[] key = Enumerable.Range(0, Blake2b.MaxKeySize).Select(i => (byte)i).ToArray();
        const string expected = "10EBB67700B1868EFB4417987ACF4690AE9D972FB7A590C2F02871799AAE479800CEED913CAF1ABF21401902C53ABB8D9DA3B8FAA8D00A3ACED57B2B2F0CE768";

        using var sut = new Blake2b(512) { Key = key };
        byte[] digest = sut.ComputeHash([]);

        Assert.AreEqual(expected, Convert.ToHexString(digest));
    }
}
