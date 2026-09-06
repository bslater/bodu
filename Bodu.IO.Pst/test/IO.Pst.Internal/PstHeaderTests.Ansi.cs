// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeaderTests.Ansi.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Pst.Internal;

public partial class PstHeaderTests
{
    /// <summary>
    /// Verifies that the ANSI corpus header parses with the values the file actually carries: the format, the
    /// permute encoding, the file length, and both B-tree roots at their 32-bit offsets.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAnsiCorpusHeader_ShouldReadThirtyTwoBitRoot()
    {
        byte[] bytes = PstReferenceFixtures.OpenStream(PstFileTests.Sample2Ansi).ToArray();

        PstHeader header = PstHeader.Parse(bytes.AsSpan(0, PstLayout.Ansi.HeaderSize), PstValidationLevel.Strict);

        Assert.AreEqual(PstFileFormat.Ansi, header.Format);
        Assert.AreEqual(PstCryptMethod.Permute, header.CryptMethod);
        Assert.AreEqual(271_360L, header.FileLength);
        Assert.AreEqual(new PstBref(999, 0x8800), header.NbtRoot);
        Assert.AreEqual(new PstBref(1002, 0x7000), header.BbtRoot);
    }

    /// <summary>
    /// Verifies that both ANSI version numbers open a synthetic ANSI store and report the ANSI format.
    /// </summary>
    [TestMethod]
    [DataRow(14)]
    [DataRow(15)]
    public void Parse_WhenVersionIsAnsi_ShouldOpenAndReportAnsiFormat(int version)
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi, Version = (ushort)version };
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });

        Assert.AreEqual(PstFileFormat.Ansi, file.Format);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, file.GetNode(new PstNodeId(0x21)).ReadAllBytes());
    }

    /// <summary>
    /// Verifies that an ANSI header is validated at its own offsets: a wrong sentinel and a wrong checksum are refused
    /// as malformed, and an unknown crypt method as unsupported.
    /// </summary>
    [TestMethod]
    public void Parse_WhenAnsiHeaderIsCorrupt_ShouldFailAtAnsiOffsets()
    {
        Assert.IsInstanceOfType<PstFileFormatException>(OpenAndCatch(new PstFixtureBuilder { Format = PstFileFormat.Ansi, Sentinel = 0x00 }));
        Assert.IsInstanceOfType<PstFileFormatException>(OpenAndCatch(new PstFixtureBuilder { Format = PstFileFormat.Ansi, WriteValidHeaderCrc = false }));
        Assert.IsNull(OpenAndCatch(new PstFixtureBuilder { Format = PstFileFormat.Ansi, WriteValidHeaderCrc = false }, PstValidationLevel.Minimal));
        Assert.IsInstanceOfType<PstUnsupportedFormatException>(OpenAndCatch(new PstFixtureBuilder { Format = PstFileFormat.Ansi, RawCryptMethod = 0x03 }));
    }

    /// <summary>
    /// Verifies that each content encoding round-trips through an ANSI store.
    /// </summary>
    [TestMethod]
    [DataRow(PstCryptMethod.None)]
    [DataRow(PstCryptMethod.Permute)]
    [DataRow(PstCryptMethod.Cyclic)]
    public void Parse_WhenAnsi_ShouldSurfaceTheDeclaredCryptMethod(PstCryptMethod cryptMethod)
    {
        var builder = new PstFixtureBuilder { Format = PstFileFormat.Ansi, CryptMethod = cryptMethod };
        byte[] payload = [.. Enumerable.Range(0, 300).Select(static i => (byte)(i * 7))];
        builder.AddNode(0x21, builder.AddDataBlock(payload));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        Assert.AreEqual(cryptMethod, file.CryptMethod);
        Assert.AreEqual(PstFileFormat.Ansi, file.Format);
        CollectionAssert.AreEqual(payload, file.GetNode(new PstNodeId(0x21)).ReadAllBytes());
    }
}
