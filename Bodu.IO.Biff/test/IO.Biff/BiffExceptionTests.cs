// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BiffExceptionTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.IO.Biff;

/// <summary>
/// Tests for the exception types of the codec: their base types and the diagnostic properties they carry.
/// </summary>
[TestClass]
public sealed class BiffExceptionTests
{
    /// <summary>
    /// Verifies that the format exception derives from <see cref="FormatException" /> and carries its offset.
    /// </summary>
    [TestMethod]
    public void BiffFormatException_WhenConstructedWithOffset_ShouldExposeOffset()
    {
        var ex = new BiffFormatException("bad", 42);

        Assert.IsInstanceOfType<FormatException>(ex);
        Assert.AreEqual("bad", ex.Message);
        Assert.AreEqual(42, ex.Offset);
        Assert.IsNull(new BiffFormatException("bad").Offset);
        Assert.IsNull(new BiffFormatException().Offset);
        Assert.AreSame(ex, new BiffFormatException("outer", ex).InnerException);
    }

    /// <summary>
    /// Verifies that the unsupported-version exception derives from <see cref="NotSupportedException" /> and carries
    /// the raw marker.
    /// </summary>
    [TestMethod]
    public void BiffUnsupportedVersionException_WhenConstructedWithMarker_ShouldExposeRawVersion()
    {
        var ex = new BiffUnsupportedVersionException("old", 0x0400);

        Assert.IsInstanceOfType<NotSupportedException>(ex);
        Assert.AreEqual((ushort)0x0400, ex.RawVersion);
        Assert.IsNull(new BiffUnsupportedVersionException("old").RawVersion);
        Assert.IsNull(new BiffUnsupportedVersionException().RawVersion);
        Assert.AreSame(ex, new BiffUnsupportedVersionException("outer", ex).InnerException);
    }
}
