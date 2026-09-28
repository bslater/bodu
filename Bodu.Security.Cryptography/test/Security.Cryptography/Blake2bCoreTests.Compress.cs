// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2bCoreTests.Compress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public sealed partial class Blake2bCoreTests
{
    /// <summary>
    /// Verifies that each kernel reproduces RFC 7693, Appendix A's worked example, BLAKE2b-512("abc").
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    public void Compress_ForEachKernel_ShouldMatchRfc7693Example(string kernel)
    {
        byte[] digest = Hash(ParseSupportedKernel(kernel), [], Encoding.ASCII.GetBytes("abc"), 64);

        Assert.AreEqual("ba80a53f981c4d0d6a2797b69f12f6e94c212f14685ac4b74b12bb6fdbffa2d17d87c5392aab792dc252d5de4533cc9518d38aa8dbf1925ab92386edd4009923", Convert.ToHexString(digest).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that each kernel reproduces every BLAKE2b entry of the official blake2-kat.json, unkeyed and keyed,
    /// over messages of 0 to 255 bytes.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    public void Compress_ForEachKernel_ShouldMatchReferenceVectors(string kernel)
    {
        Blake2bCore.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (MessageDigestKnownAnswer vector in ReadReferenceVectors())
        {
            byte[] digest = Hash(kind, vector.Key ?? [], vector.Message, vector.Digest.Length);

            CollectionAssert.AreEqual(vector.Digest, digest, vector.Name);
        }
    }
}
