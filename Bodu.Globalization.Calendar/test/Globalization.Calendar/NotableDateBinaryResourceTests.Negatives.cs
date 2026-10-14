// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.Negatives.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Security.Cryptography;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>
    /// Rewrites the header's payload digest so a deliberately mutated payload still passes the integrity check and
    /// reaches structural validation.
    /// </summary>
    /// <param name="pack">The pack whose digest is recomputed in place.</param>
    private static void RehashPayload(byte[] pack) =>
        SHA256.HashData(pack.AsSpan(NotableDateBinaryFormat.HeaderLength), pack.AsSpan(8, SHA256.HashSizeInBytes));

    /// <summary>
    /// Verifies that a pack whose fixed strategy carries a Hebrew month alias in a calendar other than the Hebrew, which
    /// the loader never reads, is rejected with <see cref="NotableDateBinaryFormatException" /> carrying the strategy's
    /// <see cref="ArgumentException" />, rather than read as a rule that resolves the name in another calendar.
    /// </summary>
    [TestMethod]
    public void Read_WhenFixedStrategyCarriesMonthAliasOutsideHebrew_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(FixedRuleResource("Hebrew", "Nisan", 15));

        // The fixed strategy's encoding: discriminator, month 0 and day 15 as zigzag integers, then the calendar byte.
        PatchStrategy(pack, [NotableDateBinaryFormat.StrategyFixedDate, 0x00, 0x1E, (byte)CalendarSystem.Hebrew], 3, (byte)CalendarSystem.Hijri);

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.IsInstanceOfType<ArgumentException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a pack whose fixed strategy names a month beyond the months its calendar has, which the loader
    /// rejects, is rejected with <see cref="NotableDateBinaryFormatException" /> carrying the strategy's
    /// <see cref="ArgumentOutOfRangeException" />, rather than read as a rule that never occurs.
    /// </summary>
    [TestMethod]
    public void Read_WhenFixedStrategyMonthIsBeyondTheCalendarsMonths_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(FixedRuleResource("Persian", "12", 29));

        // Month 12 is the zigzag byte 0x18 and day 29 the byte 0x3A; month 13 is 0x1A.
        PatchStrategy(pack, [NotableDateBinaryFormat.StrategyFixedDate, 0x18, 0x3A, (byte)CalendarSystem.Persian], 1, 0x1A);

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a pack whose Gregorian fixed strategy names a day its month never has, which the loader rejects with
    /// <c>BODU-CAL-DAY</c>, is rejected with <see cref="NotableDateBinaryFormatException" /> carrying the strategy's
    /// <see cref="ArgumentOutOfRangeException" />, rather than read as a rule that never occurs.
    /// </summary>
    [TestMethod]
    public void Read_WhenFixedStrategyDayIsBeyondTheGregorianMonthsLongest_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(FixedRuleResource("Gregorian", "2", 29));

        // Month 2 is the zigzag byte 0x04 and day 29 the byte 0x3A; day 30 is 0x3C.
        PatchStrategy(pack, [NotableDateBinaryFormat.StrategyFixedDate, 0x04, 0x3A, (byte)CalendarSystem.Gregorian], 2, 0x3C);

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.IsInstanceOfType<ArgumentOutOfRangeException>(ex.InnerException);
    }

    /// <summary>
    /// Verifies that a pack whose strategy carries a weekday value <see cref="DayOfWeek" /> does not define, which the
    /// loader rejects with <c>BODU-CAL-ENUM</c> and the strategy's constructor accepts, is rejected with
    /// <see cref="NotableDateBinaryFormatException" /> by the reader's own enumeration check.
    /// </summary>
    /// <param name="strategy">The strategy element, its weekday Monday.</param>
    /// <param name="discriminator">The strategy's discriminator byte.</param>
    /// <param name="encodedMonth">The strategy's month as its zigzag byte.</param>
    /// <param name="weekdayOffset">The offset of the weekday byte from the discriminator.</param>
    [TestMethod]
    [DataRow("<DayOfWeekInMonth month=\"5\" dayOfWeek=\"Monday\" weekOrdinal=\"First\" />", NotableDateBinaryFormat.StrategyDayOfWeekInMonth, (byte)0x0A, 2)]
    [DataRow("<WeekdayNearDate month=\"5\" day=\"24\" dayOfWeek=\"Monday\" direction=\"After\" />", NotableDateBinaryFormat.StrategyWeekdayNearDate, (byte)0x0A, 3)]
    [DataRow("<RelativeWeekdayInMonth month=\"5\" dayOfWeek=\"Monday\" weekOrdinal=\"First\" relativeDayOfWeek=\"Friday\" direction=\"After\" />", NotableDateBinaryFormat.StrategyRelativeWeekdayInMonth, (byte)0x0A, 2)]
    public void Read_WhenStrategyWeekdayIsUndefined_ShouldThrowBinaryFormatException(string strategy, byte discriminator, byte encodedMonth, int weekdayOffset)
    {
        byte[] pack = WritePack(StrategyRuleResource(strategy));
        byte[] encoding = weekdayOffset == 2
            ? [discriminator, encodedMonth, (byte)DayOfWeek.Monday]
            : [discriminator, encodedMonth, 0x30, (byte)DayOfWeek.Monday];

        PatchStrategy(pack, encoding, weekdayOffset, 7);

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.Contains("DayOfWeek value 7", ex.Message);
    }

    /// <summary>
    /// Builds a one-rule resource whose rule holds the supplied strategy element.
    /// </summary>
    /// <param name="strategy">The strategy element, in the document namespace by default.</param>
    /// <returns>The loaded resource.</returns>
    private static NotableDateResource StrategyRuleResource(string strategy) =>
        NotableDateResourceLoader.Load(
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.strategy">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="Observance">
                  <Rules><Rule id="r"><Strategy>{strategy}</Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """);

    /// <summary>
    /// Builds a one-rule resource whose rule is a fixed strategy in the supplied calendar.
    /// </summary>
    /// <param name="calendar">The rule's calendar.</param>
    /// <param name="month">The month token.</param>
    /// <param name="day">The day.</param>
    /// <returns>The loaded resource.</returns>
    private static NotableDateResource FixedRuleResource(string calendar, string month, int day) =>
        NotableDateResourceLoader.Load(
            $"""
            <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="test.fixed">
              <NotableDates>
                <NotableDate id="x" displayName="X" category="Religious">
                  <Rules><Rule id="r"><Applicability calendar="{calendar}" /><Strategy><Fixed month="{month}" day="{day}" sweepCalendarYears="true" /></Strategy></Rule></Rules>
                </NotableDate>
              </NotableDates>
            </NotableDateResource>
            """);

    /// <summary>
    /// Replaces one byte of the single strategy a pack's payload holds, then rehashes the payload.
    /// </summary>
    /// <param name="pack">The pack, changed in place.</param>
    /// <param name="encoding">The leading bytes of the strategy's encoding, which occur once in the payload.</param>
    /// <param name="offset">The offset within <paramref name="encoding" /> of the byte to replace.</param>
    /// <param name="value">The byte written in its place.</param>
    private static void PatchStrategy(byte[] pack, byte[] encoding, int offset, byte value)
    {
        Span<byte> payload = pack.AsSpan(NotableDateBinaryFormat.HeaderLength);
        int index = payload.IndexOf(encoding);

        Assert.IsGreaterThanOrEqualTo(0, index, "The pack holds no such strategy.");
        Assert.AreEqual(-1, payload[(index + 1)..].IndexOf(encoding), "The pack holds the strategy twice.");

        payload[index + offset] = value;
        RehashPayload(pack);
    }

    /// <summary>
    /// Verifies that input without the BCAL signature is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenMagicIsInvalid_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        pack[0] ^= 0xFF;

        _ = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });
    }

    /// <summary>
    /// Verifies that a pack declaring an unsupported format version is rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenVersionIsUnsupported_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        pack[4] = 0xFF;
        pack[5] = 0xFF;

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.IsTrue(ex.Message.Contains("version", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that a corrupted payload fails the integrity digest before any content is interpreted.
    /// </summary>
    [TestMethod]
    public void Read_WhenPayloadCorrupted_ShouldThrowHashMismatch()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        pack[NotableDateBinaryFormat.HeaderLength + 3] ^= 0xFF;

        var ex = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(pack);
        });

        Assert.IsTrue(ex.Message.Contains("digest", StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Verifies that truncating the pack at every possible length is rejected with the format exception - never an
    /// out-of-range access or any other exception type.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenTruncatedAtEveryLength_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());

        for (var length = 0; length < pack.Length; length++)
        {
            byte[] truncated = pack.AsSpan(0, length).ToArray();
            if (length > NotableDateBinaryFormat.HeaderLength)
                RehashPayload(truncated);

            _ = Assert.ThrowsExactly<NotableDateBinaryFormatException>(
                () =>
                {
                    _ = ReadPack(truncated);
                },
                $"truncation at {length} bytes must fail cleanly");
        }
    }

    /// <summary>
    /// Verifies that mutating any single payload byte - with the digest recomputed so structural validation is
    /// reached - either decodes successfully or fails with the format exception; no other exception type may escape.
    /// The sweep covers undefined enum bytes, bad discriminators, out-of-range string references, and corrupted
    /// variable-length integers wherever they occur in the payload.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Read_WhenAnyPayloadByteMutated_ShouldFailCleanlyOrDecode()
    {
        byte[] pack = WritePack(ComprehensiveResource());

        for (int offset = NotableDateBinaryFormat.HeaderLength; offset < pack.Length; offset++)
        {
            byte[] mutated = (byte[])pack.Clone();
            mutated[offset] ^= 0xFF;
            RehashPayload(mutated);

            try
            {
                _ = ReadPack(mutated);
            }
            catch (NotableDateBinaryFormatException)
            {
                // The sealed format rejected the mutation - the expected failure mode.
            }
            catch (Exception ex)
            {
                Assert.Fail($"Mutation at offset {offset} escaped as {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Verifies that trailing bytes after the resource body are rejected.
    /// </summary>
    [TestMethod]
    public void Read_WhenPayloadCarriesTrailingBytes_ShouldThrowBinaryFormatException()
    {
        byte[] pack = WritePack(ComprehensiveResource());
        byte[] padded = new byte[pack.Length + 4];
        pack.CopyTo(padded, 0);
        RehashPayload(padded);

        _ = Assert.ThrowsExactly<NotableDateBinaryFormatException>(() =>
        {
            _ = ReadPack(padded);
        });
    }
}
