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

    /// <summary>
    /// Verifies that a header declaring a file length beyond the stream's actual length is refused under strict
    /// validation — the declared length is the one header fact the reader can cross-check — while the tolerant
    /// levels still open the file.
    /// </summary>
    [TestMethod]
    public void Open_WhenHeaderFileLengthExceedsStream_ShouldThrowOnlyUnderStrict()
    {
        byte[] bytes;
        using (MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1))
            bytes = fixture.ToArray();

        System.Buffers.Binary.BinaryPrimitives.WriteInt64LittleEndian(bytes.AsSpan(184), bytes.Length + 4096L);
        Bodu.IO.Pst.Internal.PstFixtureBuilder.RepairHeaderChecksum(bytes);

        using (var tolerant = PstFile.Open(new MemoryStream(bytes), new PstFileOptions()))
        {
            Assert.AreEqual(PstFileFormat.Unicode, tolerant.Format);
        }

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstFile.Open(new MemoryStream(bytes), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });
        });

        Assert.AreEqual(PstFileError.InvalidHeader, ex.Error);
    }

    /// <summary>
    /// Verifies that a container embedded after leading bytes opens relative to the stream position it was handed
    /// at, so every absolute file offset the header and trees carry is resolved against that base.
    /// </summary>
    [TestMethod]
    public void Open_WhenStreamIsPositionedPastLeadingBytes_ShouldReadRelativeToThatPosition()
    {
        byte[] bytes;
        using (MemoryStream fixture = PstReferenceFixtures.OpenStream(Sample1))
            bytes = fixture.ToArray();

        int expectedCount;
        using (var clean = PstFile.Open(new MemoryStream(bytes), new PstFileOptions()))
            expectedCount = clean.GetNode(PstNodeId.MessageStore).ReadPropertyContext().Count;

        byte[] prefixed = [.. new byte[100], .. bytes];
        using var stream = new MemoryStream(prefixed) { Position = 100 };
        using PstFile file = PstFile.Open(stream, new PstFileOptions());

        Assert.AreEqual(expectedCount, file.GetNode(PstNodeId.MessageStore).ReadPropertyContext().Count);
    }

    /// <summary>
    /// Verifies that a synthetic ANSI store opens and its node payload reads back through the 32-bit block tree.
    /// </summary>
    [TestMethod]
    public void Open_WhenSyntheticAnsiStore_ShouldReadNodePayload()
    {
        var builder = new Bodu.IO.Pst.Internal.PstFixtureBuilder { Format = PstFileFormat.Ansi };
        byte[] payload = [.. Enumerable.Range(0, 5000).Select(static i => (byte)(i % 251))];
        builder.AddNode(0x21, builder.AddDataBlock(payload));

        using PstFile file = PstFile.Open(builder.BuildStream(), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });

        Assert.AreEqual(PstFileFormat.Ansi, file.Format);
        CollectionAssert.AreEqual(payload, file.GetNode(new PstNodeId(0x21)).ReadAllBytes());
    }

    /// <summary>
    /// Verifies that the strict file-length check reads the ANSI header's 32-bit <c>ibFileEof</c> at its own offset.
    /// </summary>
    [TestMethod]
    public void Open_WhenAnsiHeaderFileLengthExceedsStream_ShouldThrowOnlyUnderStrict()
    {
        var builder = new Bodu.IO.Pst.Internal.PstFixtureBuilder { Format = PstFileFormat.Ansi };
        builder.AddNode(0x21, builder.AddDataBlock([1, 2, 3]));
        byte[] bytes = builder.Build();

        System.Buffers.Binary.BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(Bodu.IO.Pst.Internal.PstLayout.Ansi.FileLengthOffset), (uint)(bytes.Length + 4096));
        Bodu.IO.Pst.Internal.PstFixtureBuilder.RepairHeaderChecksum(bytes);

        using (var tolerant = PstFile.Open(new MemoryStream(bytes), new PstFileOptions()))
        {
            Assert.AreEqual(PstFileFormat.Ansi, tolerant.Format);
        }

        var ex = Assert.ThrowsExactly<PstFileFormatException>(() =>
        {
            _ = PstFile.Open(new MemoryStream(bytes), new PstFileOptions { ValidationLevel = PstValidationLevel.Strict });
        });

        Assert.AreEqual(PstFileError.InvalidHeader, ex.Error);
    }
}
