// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeaderTests.Parse.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

public partial class PstHeaderTests
{
    /// <summary>
    /// Verifies that a header version of 23 or more other than 36 declares the Unicode format, as MS-PST 2.2.2.6
    /// defines it: such a file opens under strict validation, reports <see cref="PstFileFormat.Unicode" />, and reads.
    /// Version 37 is a Unicode file written by an Outlook that supports Windows Information Protection.
    /// </summary>
    /// <param name="version">The header's <c>wVer</c>.</param>
    [TestMethod]
    [DataRow(24)]
    [DataRow(35)]
    [DataRow(37)]
    [DataRow(65535)]
    public void Parse_WhenVersionIsAboveTheFirstUnicodeVersion_ShouldOpenAndReportUnicodeFormat(int version)
    {
        var builder = new PstFixtureBuilder { Version = (ushort)version };
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });

        Assert.AreEqual(PstFileFormat.Unicode, file.Format);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3 }, file.GetNode(new PstNodeId(0x21)).ReadAllBytes());
    }

    /// <summary>
    /// Verifies that a header declaring Windows Information Protection encryption (<c>bCryptMethod</c> 0x10) is refused
    /// as an unsupported content encoding at the WIP-capable version 37 as at version 23, rather than as a 4 KiB-page
    /// OST.
    /// </summary>
    /// <param name="version">The header's <c>wVer</c>.</param>
    [TestMethod]
    [DataRow(23)]
    [DataRow(37)]
    public void Parse_WhenCryptMethodIsWindowsInformationProtection_ShouldThrowPstUnsupportedFormatException(int version)
    {
        var builder = new PstFixtureBuilder { Version = (ushort)version, RawCryptMethod = 0x10 };
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));

        var ex = Assert.ThrowsExactly<PstUnsupportedFormatException>(() =>
        {
            using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);
        });

        Assert.AreEqual(PstFileError.UnsupportedFormat, ex.Error);
        StringAssert.Contains(ex.Message, "0x10", StringComparison.Ordinal);
    }
}
