// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstHeaderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Pst;

namespace Bodu.IO.Pst.Internal;

/// <summary>
/// Verifies the header's format discrimination and its per-level checksum policy.
/// </summary>
/// <remarks>
/// The corpus covers the two variants it contains - Unicode, which opens, and ANSI, which is refused - but not the
/// 4&#160;KiB-page variant, an unrecognized version, a wrong sentinel, or an unsupported content encoding. Those decide
/// whether a caller sees "this variant is not supported yet" or "this file is damaged", a distinction that matters to
/// anyone handling the exception, so each is asserted by exact type.
/// </remarks>
[TestClass]
public partial class PstHeaderTests
{
    /// <summary>
    /// Opens a container built from the supplied builder and returns the exception it produced.
    /// </summary>
    /// <param name="builder">The configured builder.</param>
    /// <param name="validationLevel">The validation level to open at.</param>
    /// <returns>The exception thrown, or <see langword="null" /> when the container opened.</returns>
    private static Exception? OpenAndCatch(PstFixtureBuilder builder, PstValidationLevel validationLevel = PstValidationLevel.Compatible)
    {
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));

        try
        {
            using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = validationLevel });
            return null;
        }
        catch (Exception ex)
        {
            return ex;
        }
    }

    /// <summary>
    /// Verifies that a header declaring the 4&#160;KiB-page variant is refused as unsupported rather than as
    /// malformed, since the file is well formed and simply not yet readable.
    /// </summary>
    [TestMethod]
    public void Parse_WhenVersionIsFourKilobytePage_ShouldThrowPstUnsupportedFormatException()
    {
        Exception? ex = OpenAndCatch(new PstFixtureBuilder { Version = 36 });

        Assert.IsInstanceOfType<PstUnsupportedFormatException>(ex);
    }

    /// <summary>
    /// Verifies that an unrecognized version is refused as malformed rather than as an unsupported variant, since
    /// there is no variant it names.
    /// </summary>
    [TestMethod]
    public void Parse_WhenVersionIsUnrecognized_ShouldThrowPstFileFormatException()
    {
        Exception? ex = OpenAndCatch(new PstFixtureBuilder { Version = 20 });

        Assert.IsInstanceOfType<PstFileFormatException>(ex);
        Assert.IsNotInstanceOfType<PstUnsupportedFormatException>(ex);
    }

    /// <summary>
    /// Verifies that a wrong sentinel byte is refused. It is the format's own marker that the second half of the
    /// header is where the layout says it is.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSentinelIsWrong_ShouldThrowPstFileFormatException()
    {
        Exception? ex = OpenAndCatch(new PstFixtureBuilder { Sentinel = 0x00 });

        Assert.IsInstanceOfType<PstFileFormatException>(ex);
    }

    /// <summary>
    /// Verifies that a content encoding outside the three the format defines is refused as unsupported, so a future
    /// or proprietary method is distinguishable from corruption.
    /// </summary>
    [TestMethod]
    public void Parse_WhenCryptMethodIsUnknown_ShouldThrowPstUnsupportedFormatException()
    {
        Exception? ex = OpenAndCatch(new PstFixtureBuilder { RawCryptMethod = 0x03 });

        Assert.IsInstanceOfType<PstUnsupportedFormatException>(ex);
    }

    /// <summary>
    /// Verifies that a wrong header checksum is refused at the default validation level.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderChecksumIsWrong_ShouldThrowPstFileFormatException()
    {
        Exception? ex = OpenAndCatch(new PstFixtureBuilder { WriteValidHeaderCrc = false });

        Assert.IsInstanceOfType<PstFileFormatException>(ex);
    }

    /// <summary>
    /// Verifies that the header checksum is not consulted under minimal validation, which is the concession that
    /// level exists to make: a file whose header checksum has drifted still opens.
    /// </summary>
    [TestMethod]
    public void Parse_WhenHeaderChecksumIsWrong_ForMinimal_ShouldOpen()
    {
        Assert.IsNull(OpenAndCatch(new PstFixtureBuilder { WriteValidHeaderCrc = false }, PstValidationLevel.Minimal));
    }

    /// <summary>
    /// Verifies that the declared content encoding is surfaced on the open session, for each of the three methods.
    /// </summary>
    /// <param name="cryptMethod">The encoding the container declares.</param>
    [TestMethod]
    [DataRow(PstCryptMethod.None)]
    [DataRow(PstCryptMethod.Permute)]
    [DataRow(PstCryptMethod.Cyclic)]
    public void Parse_ShouldSurfaceTheDeclaredCryptMethod(PstCryptMethod cryptMethod)
    {
        var builder = new PstFixtureBuilder { CryptMethod = cryptMethod };
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));

        using PstFile file = PstFile.Open(builder.BuildStream(), PstFileOptions.Default);

        Assert.AreEqual(cryptMethod, file.CryptMethod);
        Assert.AreEqual(PstFileFormat.Unicode, file.Format);
    }
}
