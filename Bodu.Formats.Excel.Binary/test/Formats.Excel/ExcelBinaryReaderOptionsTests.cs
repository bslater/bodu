// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExcelBinaryReaderOptionsTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Formats.Excel;

/// <summary>
/// Verifies the behavior of <see cref="ExcelBinaryReaderOptions" />, the open-and-read configuration record.
/// </summary>
[TestClass]
public class ExcelBinaryReaderOptionsTests
{
    /// <summary>
    /// Verifies that a freshly constructed options instance reads the full metadata surface and owns the source stream.
    /// </summary>
    [TestMethod]
    public void Defaults_WhenConstructed_ShouldReadMetadataAndOwnStream()
    {
        ExcelBinaryReaderOptions options = new();

        Assert.IsFalse(options.LeaveOpen);
        Assert.IsTrue(options.ReadDocumentProperties);
        Assert.IsTrue(options.DetectDateFormats);
    }

    /// <summary>
    /// Verifies that each option can be set through its initializer.
    /// </summary>
    [TestMethod]
    public void Init_WhenSet_ShouldRetainValues()
    {
        ExcelBinaryReaderOptions options = new()
        {
            LeaveOpen = true,
            ReadDocumentProperties = false,
            DetectDateFormats = false,
        };

        Assert.IsTrue(options.LeaveOpen);
        Assert.IsFalse(options.ReadDocumentProperties);
        Assert.IsFalse(options.DetectDateFormats);
    }
}
