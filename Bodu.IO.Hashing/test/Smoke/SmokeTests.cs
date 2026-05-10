// ---------------------------------------------------------------------------------------------------------------
// <copyright file="SmokeTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
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
    /// Verifies that <see cref="Crc.ComputeHash(System.ReadOnlySpan{byte})" /> reproduces the published
    /// CRC-32/ISO-HDLC check value for the reference input <c>"123456789"</c>.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Crc_ComputeHash_ForCrc32IsoHdlcReferenceInput_ShouldMatchPublishedCheck()
    {
        Crc crc = new(CrcStandard.Get(CrcStandards.CRC32_ISOHDLC));
        var digest = crc.ComputeHash(Encoding.ASCII.GetBytes("123456789"));

        Assert.AreEqual(4, digest.Length);
        var actual = (uint)digest[0] | ((uint)digest[1] << 8) | ((uint)digest[2] << 16) | ((uint)digest[3] << 24);
        Assert.AreEqual(0xCBF43926u, actual);
    }

    /// <summary>
    /// Verifies that <see cref="Fletcher16" /> produces a 2-byte digest from a non-empty input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Fletcher16_Append_ShouldProduceTwoByteDigest()
    {
        Fletcher16 fletcher = new();
        fletcher.Append(Encoding.ASCII.GetBytes("abcde"));
        var digest = fletcher.GetCurrentHash();

        Assert.AreEqual(2, digest.Length);
        Assert.IsTrue(digest[0] != 0 || digest[1] != 0, "Non-empty input should produce a non-zero Fletcher-16 digest.");
    }

    /// <summary>
    /// Verifies that <see cref="Fletcher32" /> produces a 4-byte digest from a non-empty input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Fletcher32_Append_ShouldProduceFourByteDigest()
    {
        Fletcher32 fletcher = new();
        fletcher.Append(Encoding.ASCII.GetBytes("abcdefgh"));
        var digest = fletcher.GetCurrentHash();

        Assert.AreEqual(4, digest.Length);
    }

    /// <summary>
    /// Verifies that <see cref="Adler32" /> produces a 4-byte digest from a non-empty input.
    /// </summary>
    [TestMethod]
    [TestCategory("Smoke")]
    public void Adler32_Append_ShouldProduceFourByteDigest()
    {
        Adler32 adler = new();
        adler.Append(Encoding.ASCII.GetBytes("Wikipedia"));
        var digest = adler.GetCurrentHash();

        Assert.AreEqual(4, digest.Length);
    }
}
