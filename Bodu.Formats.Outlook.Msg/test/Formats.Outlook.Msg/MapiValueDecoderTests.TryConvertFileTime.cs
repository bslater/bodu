// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MapiValueDecoderTests.TryConvertFileTime.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

#if OUTLOOK_PST
namespace Bodu.Formats.Outlook.Pst;
#else
namespace Bodu.Formats.Outlook.Msg;
#endif

public partial class MapiValueDecoderTests
{
    /// <summary>
    /// Verifies that a FILETIME converts to a UTC time stamp — a zero offset and the same instant — regardless of the
    /// process-local time zone: MAPI time stamps are UTC by definition, and a machine-local offset would make the
    /// decoded value differ per host.
    /// </summary>
    [TestMethod]
    public void TryConvertFileTime_WhenConvertedUnderNonUtcLocalZone_ShouldReportUtcOffset()
    {
        var expected = new DateTimeOffset(2020, 1, 1, 12, 30, 0, TimeSpan.Zero);
        ulong raw = (ulong)expected.ToFileTime();

        WithLocalTimeZone("Pacific/Kiritimati", () =>
        {
            Assert.IsTrue(MapiValueDecoder.TryConvertFileTime(raw, out DateTimeOffset value));
            Assert.AreEqual(TimeSpan.Zero, value.Offset, "A decoded time stamp must carry the UTC offset.");
            Assert.AreEqual(expected, value);
            Assert.AreEqual(expected.DateTime, value.DateTime);
        });
    }

    /// <summary>
    /// Verifies that a FILETIME near the top of the representable range converts under every local time zone —
    /// acceptance must not depend on whether the host's UTC offset pushes a local-time conversion out of range.
    /// </summary>
    [TestMethod]
    public void TryConvertFileTime_WhenNearMaximumUnderPositiveLocalZone_ShouldStillConvert()
    {
        ulong raw = (ulong)DateTime.MaxValue.ToFileTimeUtc();

        WithLocalTimeZone("Pacific/Kiritimati", () =>
        {
            Assert.IsTrue(MapiValueDecoder.TryConvertFileTime(raw, out DateTimeOffset value));
            Assert.AreEqual(TimeSpan.Zero, value.Offset);
            Assert.AreEqual(DateTime.MaxValue.Year, value.Year);
        });
    }
}
