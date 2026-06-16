// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HashingStreamSmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.IO.Hashing;

namespace Bodu.Smoke;

/// <summary>
/// Provides smoke-tier coverage for <see cref="HashingStream" />: one happy-path hashing-while-copying transfer so
/// catastrophic breakage is caught by the smallest possible build run.
/// </summary>
[TestClass]
public sealed class HashingStreamSmokeTests
{
    /// <summary>
    /// Verifies that copying a payload through a <see cref="HashingStream" /> yields the same digest as hashing
    /// the payload directly.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void HashingStream_CopyThrough_ShouldMatchDirectDigest()
    {
        byte[] payload = Encoding.ASCII.GetBytes("Hashing while copying with HashingStream.");
        Fnv1a32 reference = new();
        reference.Append(payload);

        using MemoryStream source = new(payload);
        using HashingStream stream = new(source, new Fnv1a32());
        stream.CopyTo(Stream.Null);

        CollectionAssert.AreEqual(reference.GetCurrentHash(), stream.GetCurrentHash());
    }
}
