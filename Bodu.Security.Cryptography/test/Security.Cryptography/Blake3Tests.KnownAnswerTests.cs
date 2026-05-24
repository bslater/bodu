// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake3Tests.KnownAnswerTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Locks the <see cref="Blake3" /> implementation against the official BLAKE3 reference test vectors. Inputs follow
/// the convention from the BLAKE3 team's <c>test_vectors.json</c>: byte at position <c>i</c> equals <c>i % 251</c>.
/// </summary>
public partial class Blake3Tests
{
    /// <summary>
    /// Selected BLAKE3 reference vectors covering single-block, single-chunk, multi-chunk balanced (powers of two),
    /// and multi-chunk unbalanced tree shapes — taken from the official BLAKE3 team
    /// <c>test_vectors.json</c> at <see href="https://github.com/BLAKE3-team/BLAKE3/blob/master/test_vectors/test_vectors.json" />.
    /// Each <c>expected</c> value is the first 32 bytes of the extended-output reference digest.
    /// </summary>
    private static IEnumerable<object[]> Blake3ReferenceVectors() => new[]
    {
        new object[] { 0,      "af1349b9f5f9a1a6a0404dea36dcc9499bcb25c9adc112b7cc9a93cae41f3262" },
        new object[] { 1,      "2d3adedff11b61f14c886e35afa036736dcd87a74d27b5c1510225d0f592e213" },
        new object[] { 64,     "4eed7141ea4a5cd4b788606bd23f46e212af9cacebacdc7d1f4c6dc7f2511b98" },
        new object[] { 65,     "de1e5fa0be70df6d2be8fffd0e99ceaa8eb6e8c93a63f2d8d1c30ecb6b263dee" },
        new object[] { 1024,   "42214739f095a406f3fc83deb889744ac00df831c10daa55189b5d121c855af7" },
        new object[] { 1025,   "d00278ae47eb27b34faecf67b4fe263f82d5412916c1ffd97c8cb7fb814b8444" },
        new object[] { 2048,   "e776b6028c7cd22a4d0ba182a8bf62205d2ef576467e838ed6f2529b85fba24a" },
        new object[] { 2049,   "5f4d72f40d7a5f82b15ca2b2e44b1de3c2ef86c426c95c1af0b6879522563030" },
        new object[] { 3072,   "b98cb0ff3623be03326b373de6b9095218513e64f1ee2edd2525c7ad1e5cffd2" },
        new object[] { 4096,   "015094013f57a5277b59d8475c0501042c0b642e531b0a1c8f58d2163229e969" },
        new object[] { 5120,   "9cadc15fed8b5d854562b26a9536d9707cadeda9b143978f319ab34230535833" },
        new object[] { 8192,   "aae792484c8efe4f19e2ca7d371d8c467ffb10748d8a5a1ae579948f718a2a63" },
        new object[] { 16384,  "f875d6646de28985646f34ee13be9a576fd515f76b5b0a26bb324735041ddde4" },
        new object[] { 31744,  "62b6960e1a44bcc1eb1a611a8d6235b6b4b78f32e7abc4fb4c6cdcce94895c47" },
        new object[] { 102400, "bc3e3d41a1146b069abffad3c0d44860cf664390afce4d9661f7902e7943e085" },
    };

    /// <summary>
    /// Verifies that <see cref="Blake3.ComputeHash(byte[])" /> produces the digest specified by the official BLAKE3
    /// reference test vectors for inputs spanning single-block, single-chunk, multi-chunk balanced, and multi-chunk
    /// unbalanced tree shapes.
    /// </summary>
    /// <param name="inputLen">The length, in bytes, of the deterministic reference input.</param>
    /// <param name="expectedHex">The expected 32-byte digest, encoded as a lowercase hex string.</param>
    [TestMethod]
    [DynamicData(nameof(Blake3ReferenceVectors), DynamicDataSourceType.Method)]
    [TestCategory("Regression")]
    public void ComputeHash_WhenGivenOfficialBlake3ReferenceVector_ShouldMatchExpectedDigest(int inputLen, string expectedHex)
    {
        var input = new byte[inputLen];
        for (var i = 0; i < inputLen; i++)
            input[i] = (byte)(i % 251);

        using var hasher = new Blake3();
        byte[] actual = hasher.ComputeHash(input);

        Assert.AreEqual(expectedHex, Convert.ToHexString(actual).ToLowerInvariant(),
            $"BLAKE3 digest for input_len={inputLen} must match the official reference vector.");
    }
}
