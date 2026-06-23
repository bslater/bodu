// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Biff8WorkbookReaderTests.Encryption.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel.Binary;

public partial class Biff8WorkbookReaderTests
{
    /// <summary>
    /// Verifies that a globals substream containing a file-pass record is reported as an encrypted workbook.
    /// </summary>
    [TestMethod]
    public void ValidateNotEncrypted_WhenFilePassPresent_ShouldThrowEncryptedWorkbookException()
    {
        List<Biff8Record> records =
        [
            new((ushort)Biff8RecordType.Bof, new byte[] { 0x00, 0x06 }),
            new((ushort)Biff8RecordType.FilePass, new byte[4]),
            new((ushort)Biff8RecordType.Eof, default),
        ];
        List<(int Start, int EndExclusive)> substreams = [(0, records.Count)];

        _ = Assert.ThrowsExactly<Biff8EncryptedWorkbookException>(() =>
        {
            Biff8WorkbookReader.ValidateNotEncrypted(records, substreams);
        });
    }

    /// <summary>
    /// Verifies that a globals substream without a file-pass record is not reported as encrypted.
    /// </summary>
    [TestMethod]
    public void ValidateNotEncrypted_WhenNoFilePass_ShouldNotThrow()
    {
        List<Biff8Record> records =
        [
            new((ushort)Biff8RecordType.Bof, new byte[] { 0x00, 0x06 }),
            new((ushort)Biff8RecordType.Eof, default),
        ];
        List<(int Start, int EndExclusive)> substreams = [(0, records.Count)];

        Biff8WorkbookReader.ValidateNotEncrypted(records, substreams);
    }

    /// <summary>
    /// Verifies that the unencrypted sample workbook opens without being reported as encrypted.
    /// </summary>
    [TestMethod]
    public void Open_WhenSampleWorkbook_ShouldNotThrowEncryptedWorkbookException()
    {
        Biff8WorkbookReader reader = OpenSample();

        Assert.IsNotNull(reader);
    }
}
