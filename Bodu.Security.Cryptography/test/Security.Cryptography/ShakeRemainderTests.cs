// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ShakeRemainderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Security.Cryptography;

/// <summary>
/// Covers the Keccak squeeze path that serializes a sub-lane tail when the requested output length is not a multiple of
/// eight bytes.
/// </summary>
[TestClass]
public sealed class ShakeRemainderTests
{
    /// <summary>
    /// Verifies that a SHAKE128 output length that is not a whole number of 8-byte lanes produces the requested number
    /// of bytes, exercising the sub-lane tail serialization.
    /// </summary>
    [TestMethod]
    public void ComputeHash_WhenOutputLengthHasSubLaneTail_ShouldProduceRequestedLength()
    {
        byte[] input = System.Text.Encoding.ASCII.GetBytes("the quick brown fox");

        using Shake shake = new(outputBits: 160, securityLevel: 128);
        byte[] hash = shake.ComputeHash(input);

        Assert.AreEqual(20, hash.Length);
        Assert.IsTrue(hash.Any(b => b != 0));
    }
}
