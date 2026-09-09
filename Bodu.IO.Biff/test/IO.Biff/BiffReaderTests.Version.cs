// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffReaderTests.Version.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

public sealed partial class BiffReaderTests
{
    /// <summary>
    /// Verifies that a BIFF8 BOF record establishes the version.
    /// </summary>
    [TestMethod]
    public void Version_WhenBiff8Bof_ShouldBeBiff8()
    {
        var reader = new BiffReader(BiffTestRecords.Bof8());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffVersion.Biff8, reader.Version);
        Assert.AreEqual(BiffVersion.Biff8, reader.CurrentState.Version);
    }

    /// <summary>
    /// Verifies that a BIFF5 BOF record establishes the version.
    /// </summary>
    [TestMethod]
    public void Version_WhenBiff5Bof_ShouldBeBiff5()
    {
        var reader = new BiffReader(BiffTestRecords.Bof5());

        Assert.IsTrue(reader.Read());
        Assert.AreEqual(BiffVersion.Biff5, reader.Version);
    }

    /// <summary>
    /// Verifies that a BOF record declaring a version other than BIFF5 or BIFF8 is rejected as unsupported, carrying
    /// the raw marker.
    /// </summary>
    /// <param name="marker">The version marker.</param>
    [TestMethod]
    [DataRow((ushort)0x0000)]
    [DataRow((ushort)0x0400)]
    [DataRow((ushort)0x0700)]
    public void Version_WhenBofDeclaresUnsupportedVersion_ShouldThrowBiffUnsupportedVersionException(ushort marker)
    {
        byte[] stream = BiffTestRecords.Bof(marker);

        var ex = Assert.ThrowsExactly<BiffUnsupportedVersionException>(() =>
        {
            var reader = new BiffReader(stream);
            _ = reader.Read();
        });

        Assert.AreEqual(marker, ex.RawVersion);
    }

    /// <summary>
    /// Verifies that a stream opening with a BIFF2, BIFF3, or BIFF4 beginning-of-file record is rejected as
    /// unsupported rather than malformed.
    /// </summary>
    /// <param name="legacyBof">The legacy BOF identifier.</param>
    [TestMethod]
    [DataRow((ushort)0x0009)]
    [DataRow((ushort)0x0209)]
    [DataRow((ushort)0x0409)]
    public void Version_WhenStreamOpensWithLegacyBof_ShouldThrowBiffUnsupportedVersionException(ushort legacyBof)
    {
        byte[] stream = BiffTestRecords.Record(legacyBof, new byte[4]);

        var ex = Assert.ThrowsExactly<BiffUnsupportedVersionException>(() =>
        {
            var reader = new BiffReader(stream);
            _ = reader.Read();
        });

        Assert.AreEqual(legacyBof, ex.RawVersion);
    }

    /// <summary>
    /// Verifies that a legacy BOF identifier appearing after the version has been established is treated as an
    /// ordinary record rather than a version marker.
    /// </summary>
    [TestMethod]
    public void Version_WhenLegacyBofIdAppearsAfterVersionEstablished_ShouldReadAsOrdinaryRecord()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Record(0x0009, [1, 2]));
        var reader = new BiffReader(stream);

        Assert.IsTrue(reader.Read());
        Assert.IsTrue(reader.Read());
        Assert.AreEqual(0x0009, reader.RecordId);
    }

    /// <summary>
    /// Verifies that a later BOF record agreeing with the established version is accepted.
    /// </summary>
    [TestMethod]
    public void Version_WhenSheetBofAgrees_ShouldRemainEstablished()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Eof(), BiffTestRecords.Bof8(BiffSubstreamType.Worksheet));
        var reader = new BiffReader(stream);

        while (reader.Read())
        {
        }

        Assert.AreEqual(BiffVersion.Biff8, reader.Version);
    }

    /// <summary>
    /// Verifies that a later BOF record disagreeing with the established version is rejected as malformed.
    /// </summary>
    [TestMethod]
    public void Version_WhenSheetBofDisagrees_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Stream(BiffTestRecords.Bof8(), BiffTestRecords.Eof(), BiffTestRecords.Bof5(BiffSubstreamType.Worksheet));

        var ex = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var reader = new BiffReader(stream);
            while (reader.Read())
            {
            }
        });

        Assert.AreEqual(20 + 4, ex.Offset);
    }

    /// <summary>
    /// Verifies that a BOF record disagreeing with a version supplied through the options is rejected.
    /// </summary>
    [TestMethod]
    public void Version_WhenBofDisagreesWithOptions_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Bof8();

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var reader = new BiffReader(stream, new BiffReaderOptions { Version = BiffVersion.Biff5 });
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that a BOF record too short to carry a version marker is rejected as malformed.
    /// </summary>
    [TestMethod]
    public void Version_WhenBofIsTooShort_ShouldThrowBiffFormatException()
    {
        byte[] stream = BiffTestRecords.Record(BiffRecordType.Bof, [0x00]);

        _ = Assert.ThrowsExactly<BiffFormatException>(() =>
        {
            var reader = new BiffReader(stream);
            _ = reader.Read();
        });
    }

    /// <summary>
    /// Verifies that a version-dependent accessor refuses to decode before the version is known.
    /// </summary>
    [TestMethod]
    public void Version_WhenUnknownAndVersionDependentAccessorCalled_ShouldThrowInvalidOperationException()
    {
        byte[] stream = BiffTestRecords.Dimensions8(0, 1, 0, 1);

        _ = Assert.ThrowsExactly<InvalidOperationException>(() =>
        {
            var reader = new BiffReader(stream);
            _ = reader.Read();
            _ = reader.GetDimensions();
        });
    }

    /// <summary>
    /// Verifies that a version supplied through the options lets a version-dependent accessor decode a record that
    /// precedes any BOF.
    /// </summary>
    [TestMethod]
    public void Version_WhenSuppliedThroughOptions_ShouldEnableVersionDependentAccessor()
    {
        var reader = new BiffReader(BiffTestRecords.Dimensions5(1, 4, 2, 6), new BiffReaderOptions { Version = BiffVersion.Biff5 });

        Assert.IsTrue(reader.Read());
        BiffDimensionsRecord dimensions = reader.GetDimensions();
        Assert.AreEqual(1, dimensions.FirstRow);
        Assert.AreEqual(4, dimensions.LastRowExclusive);
    }
}
