// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.IO.Hashing.Checksums;

namespace Bodu.Smoke;

/// <summary>
/// Provides smoke-tier coverage for the primary public types in <c>Bodu.IO.Hashing</c>. Each test exercises one
/// happy-path entry point so that catastrophic breakage (constructor regression, lookup-table dispatch fault,
/// digest sizing error) is caught by the smallest possible build run.
/// </summary>
[TestClass]
public sealed class SmokeTests
{

    /// <summary>
    /// Verifies that <see cref="Adler32" /> produces a 4-byte digest from a non-empty input.
    /// </summary>
    [TestMethod]
    public void Adler32_Append_ShouldProduceFourByteDigest()
    {
        Adler32 adler = new();
        adler.Append(Encoding.ASCII.GetBytes("Wikipedia"));
        byte[] digest = adler.GetCurrentHash();

        Assert.HasCount(4, digest);
    }
    /// <summary>
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> reproduces the published
    /// CRC-32/ISO-HDLC check value for the reference input <c>"123456789"</c>.
    /// </summary>
    [TestMethod]
    public void Crc_ComputeHash_ForCrc32IsoHdlcReferenceInput_ShouldMatchPublishedCheck()
    {
        Crc crc = new(CrcStandard.Get(CrcStandards.CRC32_ISOHDLC));
        byte[] digest = crc.ComputeHash(Encoding.ASCII.GetBytes("123456789"));

        Assert.HasCount(4, digest);
        uint actual = (uint)digest[0] | ((uint)digest[1] << 8) | ((uint)digest[2] << 16) | ((uint)digest[3] << 24);
        Assert.AreEqual(0xCBF43926u, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Fletcher16" /> produces a 2-byte digest from a non-empty input.
    /// </summary>
    [TestMethod]
    public void Fletcher16_Append_ShouldProduceTwoByteDigest()
    {
        Fletcher16 fletcher = new();
        fletcher.Append(Encoding.ASCII.GetBytes("abcde"));
        byte[] digest = fletcher.GetCurrentHash();

        Assert.HasCount(2, digest);
        Assert.IsTrue(digest[0] != 0 || digest[1] != 0, "Non-empty input should produce a non-zero Fletcher-16 digest.");
    }

    /// <summary>
    /// Verifies that <see cref="Fletcher32" /> produces a 4-byte digest from a non-empty input.
    /// </summary>
    [TestMethod]
    public void Fletcher32_Append_ShouldProduceFourByteDigest()
    {
        Fletcher32 fletcher = new();
        fletcher.Append(Encoding.ASCII.GetBytes("abcdefgh"));
        byte[] digest = fletcher.GetCurrentHash();

        Assert.HasCount(4, digest);
    }

}
