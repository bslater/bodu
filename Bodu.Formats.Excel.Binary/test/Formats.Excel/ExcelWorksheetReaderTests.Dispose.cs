// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelWorksheetReaderTests.Dispose.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

public partial class ExcelWorksheetReaderTests
{
    /// <summary>
    /// Verifies that reading from a disposed worksheet reader throws <see cref="ObjectDisposedException" />.
    /// </summary>
    [TestMethod]
    public void TryReadCell_WhenReaderDisposed_ShouldThrowObjectDisposedException()
    {
        ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(Biff8TestWorkbook.Number(0, 0, 1.0));
        reader.Dispose();

        _ = Assert.ThrowsExactly<ObjectDisposedException>(() =>
        {
            _ = reader.TryReadCell(out _);
        });
    }

    /// <summary>
    /// Verifies that disposing a worksheet reader more than once is harmless.
    /// </summary>
    [TestMethod]
    public void Dispose_WhenCalledTwice_ShouldNotThrow()
    {
        ExcelWorksheetReader reader = Biff8TestWorkbook.OpenWorksheetReader(Biff8TestWorkbook.Number(0, 0, 1.0));

        reader.Dispose();
        reader.Dispose();
    }
}
