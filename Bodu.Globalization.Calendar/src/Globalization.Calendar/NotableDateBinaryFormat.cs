// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryFormat.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Defines the constants of the sealed notable-date binary rule-pack format (<c>.bcal</c>), shared by the writer and
/// the reader so both directions agree on every discriminator by construction.
/// </summary>
/// <remarks>
/// <para>
/// The format is deliberately closed: discriminators enumerate the engine's strategy, recurrence, and duration types
/// exhaustively, and the reader rejects any value outside these tables. Additions require a new format version.
/// </para>
/// </remarks>
internal static class NotableDateBinaryFormat
{
    /// <summary>
    /// Gets the four magic bytes opening every pack: <c>BCAL</c> in ASCII.
    /// </summary>
    public static ReadOnlySpan<byte> Magic =>
        "BCAL"u8;

    /// <summary>The current (and only) format version.</summary>
    public const ushort Version = 1;

    /// <summary>The header length in bytes: magic (4) + version (2) + flags (2) + payload SHA-256 (32).</summary>
    public const int HeaderLength = 40;

    /// <summary>The rule occurrence-source marker for a single-date calculation strategy.</summary>
    public const byte SourceStrategy = 1;

    /// <summary>The rule occurrence-source marker for a recurrence strategy.</summary>
    public const byte SourceRecurrence = 2;

    /// <summary>The duration marker for no duration override.</summary>
    public const byte DurationNone = 0;

    /// <summary>The duration marker for <see cref="FixedDurationDefinition" />.</summary>
    public const byte DurationFixed = 1;

    /// <summary>The duration marker for <see cref="CalculatedEndDateDurationDefinition" />.</summary>
    public const byte DurationCalculatedEnd = 2;

    /// <summary>Strategy discriminator: <see cref="Algorithms.FixedDateStrategy" />.</summary>
    public const byte StrategyFixedDate = 1;

    /// <summary>Strategy discriminator: <see cref="Algorithms.DayOfWeekInMonthStrategy" />.</summary>
    public const byte StrategyDayOfWeekInMonth = 2;

    /// <summary>Strategy discriminator: <see cref="Algorithms.RelativeWeekdayInMonthStrategy" />.</summary>
    public const byte StrategyRelativeWeekdayInMonth = 3;

    /// <summary>Strategy discriminator: <see cref="Algorithms.WeekdayNearDateStrategy" />.</summary>
    public const byte StrategyWeekdayNearDate = 4;

    /// <summary>Strategy discriminator: <see cref="Algorithms.WeekdayNearRuleStrategy" />.</summary>
    public const byte StrategyWeekdayNearRule = 5;

    /// <summary>Strategy discriminator: <see cref="Algorithms.OffsetFromRuleStrategy" />.</summary>
    public const byte StrategyOffsetFromRule = 6;

    /// <summary>Strategy discriminator: <see cref="Algorithms.NthWeekdayFromRuleStrategy" />.</summary>
    public const byte StrategyNthWeekdayFromRule = 7;

    /// <summary>Strategy discriminator: <see cref="Algorithms.WorkingDayOffsetFromRuleStrategy" />.</summary>
    public const byte StrategyWorkingDayOffsetFromRule = 8;

    /// <summary>Strategy discriminator: <see cref="Algorithms.WorkingDayInMonthStrategy" />.</summary>
    public const byte StrategyWorkingDayInMonth = 9;

    /// <summary>Strategy discriminator: <see cref="Algorithms.OrdinalDayOfMonthStrategy" />.</summary>
    public const byte StrategyOrdinalDayOfMonth = 10;

    /// <summary>Strategy discriminator: <see cref="Algorithms.IsoWeekDateStrategy" />.</summary>
    public const byte StrategyIsoWeekDate = 11;

    /// <summary>Strategy discriminator: <see cref="Algorithms.DayOfYearStrategy" />.</summary>
    public const byte StrategyDayOfYear = 12;

    /// <summary>Strategy discriminator: <see cref="Algorithms.AlgorithmDateStrategy" />.</summary>
    public const byte StrategyAlgorithm = 13;

    /// <summary>Recurrence discriminator: <see cref="Algorithms.DailyIntervalRecurrenceStrategy" />.</summary>
    public const byte RecurrenceDailyInterval = 1;

    /// <summary>Recurrence discriminator: <see cref="Algorithms.WeeklyRecurrenceStrategy" />.</summary>
    public const byte RecurrenceWeekly = 2;

    /// <summary>Recurrence discriminator: <see cref="Algorithms.MonthlyDayRecurrenceStrategy" />.</summary>
    public const byte RecurrenceMonthlyDay = 3;

    /// <summary>Recurrence discriminator: <see cref="Algorithms.MonthlyWeekdayRecurrenceStrategy" />.</summary>
    public const byte RecurrenceMonthlyWeekday = 4;

    /// <summary>
    /// Encodes a working week as a pack stores it: one byte, Sunday at bit 6 and Saturday at bit 0.
    /// </summary>
    /// <param name="workingWeek">The working week.</param>
    /// <returns>The stored byte.</returns>
    /// <remarks>
    /// The layout is the reverse of <see cref="DayOfWeekSet.ToUInt64" />'s order. It is fixed by format version 1, so
    /// every pack already written keeps reading.
    /// </remarks>
    public static byte EncodeWorkingWeek(DayOfWeekSet workingWeek)
    {
        int encoded = 0;
        foreach (DayOfWeek day in workingWeek)
            encoded |= 1 << (6 - (int)day);

        return (byte)encoded;
    }

    /// <summary>
    /// Decodes a working week from its stored byte, Sunday at bit 6 and Saturday at bit 0.
    /// </summary>
    /// <param name="encoded">The stored byte, with nothing set above bit 6.</param>
    /// <returns>The working week.</returns>
    public static DayOfWeekSet DecodeWorkingWeek(byte encoded)
    {
        DayOfWeekSet workingWeek = DayOfWeekSet.Empty;
        for (int day = 0; day < 7; day++)
        {
            if ((encoded & (1 << (6 - day))) != 0)
                workingWeek = workingWeek.With((DayOfWeek)day);
        }

        return workingWeek;
    }
}
