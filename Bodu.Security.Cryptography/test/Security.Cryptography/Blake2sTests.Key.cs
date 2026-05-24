// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sTests.Key.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

public partial class Blake2sTests
{
    /// <summary>
    /// Verifies that the keyed BLAKE2s-256 digest of an empty message with a 32-byte sequential key matches the
    /// known-answer value from the BLAKE2 reference test vectors (blake2s-kat.txt, first entry).
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenKeyedWithEmptyInputAndFullKey_ShouldMatchKnownReferenceVector()
    {
        // Key: 32 sequential bytes 0x00..0x1f; input: empty.
        // Reference: https://github.com/BLAKE2/BLAKE2/blob/master/testvectors/blake2s-kat.txt
        var key = Enumerable.Range(0, Blake2s.MaxKeySize / 8).Select(i => (byte)i).ToArray();
        const string expected = "48A8997DA407876B3D79C0D92325AD3B89CBB754D86AB71AEE047AD345FD2C49";

        using var sut = new Blake2s(256) { Key = key };
        var digest = sut.ComputeHash([]);

        Assert.AreEqual(expected, Convert.ToHexString(digest));
    }
}
