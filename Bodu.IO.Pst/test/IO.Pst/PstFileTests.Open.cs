// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PstFileTests.Open.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Assertions;
using Bodu.Test.IO;

namespace Bodu.IO.Pst;

public partial class PstFileTests
{
    /// <summary>
    /// Verifies the argument guards of the options overload.
    /// </summary>
    [TestMethod]
    public void Open_WhenArgumentsNull_ShouldThrowArgumentNullException()
    {
        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                _ = PstFile.Open(null!, new PstFileOptions());
            },
            "stream");

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentNullException>(
            () =>
            {
                using MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1);
                _ = PstFile.Open(fixture, null!);
            },
            "options");
    }

    /// <summary>
    /// Verifies that a non-seekable stream is rejected before any parsing.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamNotSeekable_ShouldThrowArgumentException()
    {
        byte[] bytes;
        using (MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1))
            bytes = fixture.ToArray();

        ExceptionAssert.ThrowsExactlyWithParamName<ArgumentException>(
            () =>
            {
                using var stream = new NonSeekableStream(bytes);
                _ = PstFile.Open(stream, new PstFileOptions());
            },
            "stream");
    }

    /// <summary>
    /// Verifies that the fixture opens and its full node directory walks cleanly under strict validation, so every
    /// page and block checksum and signature the file carries verifies.
    /// </summary>
    [TestMethod]
    public void Open_WhenStrictValidation_ShouldOpenAndEnumerate()
    {
        using PstFile file = PstReferenceFixtures.OpenFile(Sample1, PstValidationLevel.Strict);

        Assert.AreEqual(52, file.EnumerateNodes().Count());
    }

    /// <summary>
    /// Verifies that the fixture opens under minimal validation.
    /// </summary>
    [TestMethod]
    public void Open_WhenMinimalValidation_ShouldOpenAndEnumerate()
    {
        using PstFile file = PstReferenceFixtures.OpenFile(Sample1, PstValidationLevel.Minimal);

        Assert.AreEqual(52, file.EnumerateNodes().Count());
    }

    /// <summary>
    /// Verifies that a corrupted header checksum fails the open under the default (compatible) validation level,
    /// which verifies the header checksum, but is tolerated under minimal validation, which skips all checksums.
    /// </summary>
    [TestMethod]
    public void Open_WhenHeaderCrcCorrupted_ShouldThrowUnlessMinimal()
    {
        byte[] bytes;
        using (MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1))
            bytes = fixture.ToArray();

        // Flip a byte inside the dwCRCPartial-covered range that carries no structural meaning (wVerClient).
        bytes[12] ^= 0xFF;

        using (var corrupted = new MemoryStream(bytes))
        {
            _ = Assert.ThrowsExactly<PstFileFormatException>(() =>
            {
                _ = PstFile.Open(corrupted, new PstFileOptions());
            });
        }

        using (var corrupted = new MemoryStream(bytes))
        using (var file = PstFile.Open(corrupted, new PstFileOptions { ValidationLevel = PstValidationLevel.Minimal }))
        {
            Assert.AreEqual(PstFileFormat.Unicode, file.Format);
        }
    }
}
