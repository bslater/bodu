// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Blake2sCoreTests.Compress.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Security.Cryptography.Infrastructure;

namespace Bodu.Security.Cryptography;

public sealed partial class Blake2sCoreTests
{
    /// <summary>
    /// Verifies that each kernel reproduces RFC 7693, Appendix B's worked example, BLAKE2s-256("abc").
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    public void Compress_ForEachKernel_ShouldMatchRfc7693Example(string kernel)
    {
        byte[] digest = Hash(ParseSupportedKernel(kernel), [], Encoding.ASCII.GetBytes("abc"), 32);

        Assert.AreEqual("508c5e8c327c14e2e1a72ba34eeb452f37458b209ed63a294d999b4c86675982", Convert.ToHexString(digest).ToLowerInvariant());
    }

    /// <summary>
    /// Verifies that each kernel reproduces every BLAKE2s entry of the official blake2-kat.json, unkeyed and keyed,
    /// over messages of 0 to 255 bytes.
    /// </summary>
    /// <param name="kernel">The kernel's name.</param>
    [TestMethod]
    [DataRow("Scalar")]
    public void Compress_ForEachKernel_ShouldMatchReferenceVectors(string kernel)
    {
        Blake2sCore.KernelKind kind = ParseSupportedKernel(kernel);

        foreach (MessageDigestKnownAnswer vector in ReadReferenceVectors())
        {
            byte[] digest = Hash(kind, vector.Key ?? [], vector.Message, vector.Digest.Length);

            CollectionAssert.AreEqual(vector.Digest, digest, vector.Name);
        }
    }
}
