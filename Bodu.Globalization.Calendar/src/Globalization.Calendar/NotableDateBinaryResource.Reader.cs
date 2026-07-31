// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResource.Reader.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

public static partial class NotableDateBinaryResource
{
    /// <summary>
    /// Reads a resource from a stream in the binary rule-pack format.
    /// </summary>
    /// <param name="stream">The readable stream positioned at the start of a pack.</param>
    /// <returns>The decoded resource.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="NotableDateBinaryFormatException">
    /// The stream is not a notable-date binary rule pack, declares an unsupported format version, is truncated or
    /// corrupted, or carries a value outside the sealed format's tables.
    /// </exception>
    public static NotableDateResource Read(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        byte[] header = new byte[NotableDateBinaryFormat.HeaderLength];
        ReadExactly(stream, header);

        if (!header.AsSpan(0, 4).SequenceEqual(NotableDateBinaryFormat.Magic))
            throw new NotableDateBinaryFormatException(CalendarResourceStrings.Format_Invalid_BinaryMagic);

        ushort version = BinaryPrimitives.ReadUInt16LittleEndian(header.AsSpan(4));
        if (version != NotableDateBinaryFormat.Version)
        {
            throw new NotableDateBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Format_Invalid_BinaryVersion, version, NotableDateBinaryFormat.Version));
        }

        using MemoryStream buffered = new();
        stream.CopyTo(buffered);
        byte[] payload = buffered.ToArray();

        Span<byte> digest = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(payload, digest);
        if (!digest.SequenceEqual(header.AsSpan(8, SHA256.HashSizeInBytes)))
            throw new NotableDateBinaryFormatException(CalendarResourceStrings.Format_Invalid_BinaryHashMismatch);

        PayloadReader reader = new(payload);

        NotableDateResource resource;
        try
        {
            resource = ReadResource(reader);
        }
        catch (Exception ex) when (ex is not NotableDateBinaryFormatException
            && ex is ArgumentException or InvalidOperationException or FormatException or OverflowException)
        {
            // A decoded value escaped the format's own checks but violated a model constructor's domain guard - the
            // pack carried an out-of-domain value, which the sealed reader reports as a format violation.
            throw new NotableDateBinaryFormatException(
                string.Format(CultureInfo.CurrentCulture, CalendarResourceStrings.Format_Invalid_BinaryValue, ex.Message),
                ex);
        }

        if (!reader.AtEnd)
            throw PayloadReader.InvalidValue("trailing bytes after the resource body");

        return resource;
    }

    /// <summary>
    /// Fills a buffer from the stream, rejecting a short read as truncation.
    /// </summary>
    /// <param name="stream">The stream to read.</param>
    /// <param name="buffer">The buffer to fill.</param>
    private static void ReadExactly(Stream stream, byte[] buffer)
    {
        try
        {
            stream.ReadExactly(buffer);
        }
        catch (EndOfStreamException ex)
        {
            throw new NotableDateBinaryFormatException(CalendarResourceStrings.Format_Invalid_BinaryTruncated, ex);
        }
    }

    /// <summary>
    /// Decodes the resource body: identity, resolution policy, adjustment policies, then concepts.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded resource.</returns>
    private static NotableDateResource ReadResource(PayloadReader reader)
    {
        string resourceId = reader.ReadString();
        string schemaVersion = reader.ReadString();

        RangeResolution.ResolutionPolicy resolutionPolicy = ReadResolutionPolicy(reader);

        var policyCount = (int)reader.ReadVarUInt();
        List<AdjustmentPolicy> policies = new(Math.Min(policyCount, 1024));
        for (var i = 0; i < policyCount; i++)
            policies.Add(ReadAdjustmentPolicy(reader));

        var definitionCount = (int)reader.ReadVarUInt();
        List<NotableDateDefinition> definitions = new(Math.Min(definitionCount, 1024));
        for (var i = 0; i < definitionCount; i++)
            definitions.Add(ReadDefinition(reader));

        return new NotableDateResource(resourceId, schemaVersion, resolutionPolicy, policies, definitions);
    }

    /// <summary>
    /// Decodes the resource-level resolution policy.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded policy.</returns>
    private static RangeResolution.ResolutionPolicy ReadResolutionPolicy(PayloadReader reader)
    {
        var duplicatePolicy = reader.ReadEnum<RangeResolution.DuplicatePolicy>();
        var sameDay = reader.ReadEnum<RangeResolution.CollisionPolicy>();
        var span = reader.ReadEnum<RangeResolution.CollisionPolicy>();
        var priorityDirection = reader.ReadEnum<RangeResolution.PriorityDirection>();
        var observed = reader.ReadEnum<RangeResolution.ObservedDateRangePolicy>();
        byte workingWeek = reader.ReadByte();
        List<NotableDateCategory> precedence = reader.ReadEnumList<NotableDateCategory>();

        if (workingWeek > 0b0111_1111)
            throw PayloadReader.InvalidValue($"working-week pattern 0x{workingWeek:X2}");

        // Rebuild the pattern from its day bits (bit 6 - day, matching WeekPattern.ToByte's layout); the raw-value
        // constructor is private, so the public day-list constructor is the reconstruction path.
        List<DayOfWeek> workingDays = new(7);
        for (var day = 0; day < 7; day++)
        {
            if ((workingWeek & (1 << (6 - day))) != 0)
                workingDays.Add((DayOfWeek)day);
        }

        return new RangeResolution.ResolutionPolicy(
            duplicatePolicy,
            sameDay,
            span,
            priorityDirection,
            observed,
            new WeekPattern([.. workingDays]),
            precedence);
    }

    /// <summary>
    /// Decodes one adjustment policy with its scope and handler parameters.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded policy.</returns>
    private static AdjustmentPolicy ReadAdjustmentPolicy(PayloadReader reader)
    {
        string id = reader.ReadString();
        int priority = reader.ReadInt32();

        List<string> territories = reader.ReadStringList();
        List<CalendarSystem> calendars = reader.ReadEnumList<CalendarSystem>();
        List<NotableDateCategory> categories = reader.ReadEnumList<NotableDateCategory>();
        List<string> notableDateRefs = reader.ReadStringList();
        List<string> ruleRefs = reader.ReadStringList();
        int? scopeFromYear = reader.ReadNullableInt32();
        int? scopeToYear = reader.ReadNullableInt32();
        List<int> onlyYears = reader.ReadInt32List();
        List<int> exceptYears = reader.ReadInt32List();

        AdjustmentScope scope = new(
            territories, calendars, categories, notableDateRefs, ruleRefs,
            scopeFromYear, scopeToYear, onlyYears, exceptYears);

        var trigger = reader.ReadEnum<AdjustmentTrigger>();
        List<DayOfWeek> triggerWeekdays = reader.ReadEnumList<DayOfWeek>();
        var action = reader.ReadEnum<AdjustmentAction>();
        DayOfWeek? actionWeekday = reader.ReadNullableEnum<DayOfWeek>();
        int actionDays = reader.ReadInt32();
        int? maxSearchDays = reader.ReadNullableInt32();
        bool skipWeekends = reader.ReadBool();
        bool skipNonWorkingDates = reader.ReadBool();
        var emission = reader.ReadEnum<RangeResolution.EmissionMode>();
        string? reason = reader.ReadNullableString();
        bool? nonWorking = reader.ReadBool() ? reader.ReadBool() : null;
        int? triggerMonth = reader.ReadNullableInt32();
        int? triggerDay = reader.ReadNullableInt32();
        WeekOrdinal? triggerWeekOrdinal = reader.ReadNullableEnum<WeekOrdinal>();
        string? actionNotableDateRef = reader.ReadNullableString();
        string? actionRuleRef = reader.ReadNullableString();
        string? actionHandlerKey = reader.ReadNullableString();
        string? triggerHandlerKey = reader.ReadNullableString();

        var parameterCount = (int)reader.ReadVarUInt();
        Dictionary<string, string> handlerParameters = new(StringComparer.Ordinal);
        for (var i = 0; i < parameterCount; i++)
        {
            string key = reader.ReadString();
            handlerParameters[key] = reader.ReadString();
        }

        return new AdjustmentPolicy(
            id, priority, scope, trigger, triggerWeekdays, action, actionWeekday, actionDays, maxSearchDays,
            skipWeekends, skipNonWorkingDates, emission, reason, nonWorking, triggerMonth, triggerDay,
            triggerWeekOrdinal, actionNotableDateRef, actionRuleRef, actionHandlerKey, triggerHandlerKey,
            handlerParameters);
    }

    /// <summary>
    /// Decodes one notable-date concept with its rules.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded concept.</returns>
    private static NotableDateDefinition ReadDefinition(PayloadReader reader)
    {
        string id = reader.ReadString();
        string displayName = reader.ReadString();
        var category = reader.ReadEnum<NotableDateCategory>();
        bool defaultNonWorkingDay = reader.ReadBool();
        int defaultDurationDays = reader.ReadInt32();
        List<string> tags = reader.ReadStringList();

        var ruleCount = (int)reader.ReadVarUInt();
        List<NotableDateRule> rules = new(Math.Min(ruleCount, 1024));
        for (var i = 0; i < ruleCount; i++)
            rules.Add(ReadRule(reader));

        return new NotableDateDefinition(id, displayName, category, defaultNonWorkingDay, defaultDurationDays, tags, rules);
    }

    /// <summary>
    /// Decodes one rule with its applicability, duration, and occurrence source.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded rule.</returns>
    private static NotableDateRule ReadRule(PayloadReader reader)
    {
        string id = reader.ReadString();
        int priority = reader.ReadInt32();
        NotableDateCategory? category = reader.ReadNullableEnum<NotableDateCategory>();
        bool? nonWorking = reader.ReadBool() ? reader.ReadBool() : null;

        NotableDateDurationDefinition? duration = ReadDuration(reader);

        var calendar = reader.ReadEnum<CalendarSystem>();
        int? fromYear = reader.ReadNullableInt32();
        int? toYear = reader.ReadNullableInt32();
        List<string> territories = reader.ReadStringList();
        List<int> onlyYears = reader.ReadInt32List();
        List<int> exceptYears = reader.ReadInt32List();
        int? everyYears = reader.ReadNullableInt32();
        int? anchorYear = reader.ReadNullableInt32();

        RuleApplicability applicability = new(
            calendar, fromYear, toYear, territories, onlyYears, exceptYears, everyYears, anchorYear);

        byte source = reader.ReadByte();
        if (source == NotableDateBinaryFormat.SourceStrategy)
        {
            IDateCalculationStrategy strategy = ReadStrategy(reader);
            List<string> adjustmentRefs = reader.ReadStringList();
            List<string> tags = reader.ReadStringList();

            return new NotableDateRule(id, priority, category, nonWorking, duration, applicability, strategy, adjustmentRefs, tags);
        }

        if (source == NotableDateBinaryFormat.SourceRecurrence)
        {
            IDateRecurrenceStrategy recurrence = ReadRecurrence(reader);
            List<string> adjustmentRefs = reader.ReadStringList();
            List<string> tags = reader.ReadStringList();

            return new NotableDateRule(id, priority, category, nonWorking, duration, applicability, recurrence, adjustmentRefs, tags);
        }

        throw PayloadReader.InvalidValue($"occurrence-source marker {source}");
    }

    /// <summary>
    /// Decodes an optional duration override.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded duration, or <see langword="null" />.</returns>
    private static NotableDateDurationDefinition? ReadDuration(PayloadReader reader)
    {
        byte marker = reader.ReadByte();

        return marker switch
        {
            NotableDateBinaryFormat.DurationNone => null,
            NotableDateBinaryFormat.DurationFixed => new FixedDurationDefinition(reader.ReadInt32()),
            NotableDateBinaryFormat.DurationCalculatedEnd => new CalculatedEndDateDurationDefinition(
                ReadStrategy(reader),
                reader.ReadEnum<DateBoundary>(),
                reader.ReadEnum<DateBoundary>(),
                reader.ReadEnum<EndDateSelection>()),
            _ => throw PayloadReader.InvalidValue($"duration marker {marker}"),
        };
    }

    /// <summary>
    /// Decodes a single-date calculation strategy from its discriminator and fields.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded strategy.</returns>
    private static IDateCalculationStrategy ReadStrategy(PayloadReader reader)
    {
        byte discriminator = reader.ReadByte();

        switch (discriminator)
        {
            case NotableDateBinaryFormat.StrategyFixedDate:
            {
                int month = reader.ReadInt32();
                int day = reader.ReadInt32();
                var calendar = reader.ReadEnum<CalendarSystem>();
                bool sweep = reader.ReadBool();
                bool skipLeapMonth = reader.ReadBool();
                string? monthAlias = reader.ReadNullableString();

                return new FixedDateStrategy(month, day, calendar, sweep, skipLeapMonth, monthAlias);
            }

            case NotableDateBinaryFormat.StrategyDayOfWeekInMonth:
                return new DayOfWeekInMonthStrategy(reader.ReadInt32(), reader.ReadEnum<DayOfWeek>(), reader.ReadEnum<WeekOrdinal>());

            case NotableDateBinaryFormat.StrategyRelativeWeekdayInMonth:
                return new RelativeWeekdayInMonthStrategy(
                    reader.ReadInt32(),
                    reader.ReadEnum<DayOfWeek>(),
                    reader.ReadEnum<WeekOrdinal>(),
                    reader.ReadEnum<DayOfWeek>(),
                    reader.ReadEnum<WeekdayProximity>());

            case NotableDateBinaryFormat.StrategyWeekdayNearDate:
                return new WeekdayNearDateStrategy(
                    reader.ReadInt32(),
                    reader.ReadInt32(),
                    reader.ReadEnum<DayOfWeek>(),
                    reader.ReadEnum<WeekdayProximity>());

            case NotableDateBinaryFormat.StrategyWeekdayNearRule:
                return new WeekdayNearRuleStrategy(
                    reader.ReadString(),
                    reader.ReadNullableString(),
                    reader.ReadEnum<DayOfWeek>(),
                    reader.ReadEnum<WeekdayProximity>(),
                    reader.ReadInt32());

            case NotableDateBinaryFormat.StrategyOffsetFromRule:
                return new OffsetFromRuleStrategy(reader.ReadString(), reader.ReadNullableString(), reader.ReadInt32());

            case NotableDateBinaryFormat.StrategyNthWeekdayFromRule:
                return new NthWeekdayFromRuleStrategy(
                    reader.ReadString(),
                    reader.ReadNullableString(),
                    reader.ReadEnum<DayOfWeek>(),
                    reader.ReadInt32(),
                    reader.ReadInt32());

            case NotableDateBinaryFormat.StrategyWorkingDayOffsetFromRule:
                return new WorkingDayOffsetFromRuleStrategy(
                    reader.ReadString(),
                    reader.ReadNullableString(),
                    reader.ReadInt32(),
                    reader.ReadInt32());

            case NotableDateBinaryFormat.StrategyWorkingDayInMonth:
                return new WorkingDayInMonthStrategy(reader.ReadInt32(), reader.ReadInt32());

            case NotableDateBinaryFormat.StrategyOrdinalDayOfMonth:
                return new OrdinalDayOfMonthStrategy(reader.ReadInt32(), reader.ReadInt32());

            case NotableDateBinaryFormat.StrategyIsoWeekDate:
                return new IsoWeekDateStrategy(reader.ReadInt32(), reader.ReadEnum<DayOfWeek>());

            case NotableDateBinaryFormat.StrategyDayOfYear:
                return new DayOfYearStrategy(reader.ReadInt32());

            case NotableDateBinaryFormat.StrategyAlgorithm:
                return new AlgorithmDateStrategy(reader.ReadString());

            default:
                throw PayloadReader.InvalidValue($"strategy discriminator {discriminator}");
        }
    }

    /// <summary>
    /// Decodes a recurrence strategy from its discriminator and fields.
    /// </summary>
    /// <param name="reader">The payload reader.</param>
    /// <returns>The decoded recurrence.</returns>
    private static IDateRecurrenceStrategy ReadRecurrence(PayloadReader reader)
    {
        byte discriminator = reader.ReadByte();

        switch (discriminator)
        {
            case NotableDateBinaryFormat.RecurrenceDailyInterval:
                return new DailyIntervalRecurrenceStrategy(reader.ReadDate(), reader.ReadInt32());

            case NotableDateBinaryFormat.RecurrenceWeekly:
                return new WeeklyRecurrenceStrategy(reader.ReadEnumList<DayOfWeek>(), reader.ReadInt32(), reader.ReadNullableDate());

            case NotableDateBinaryFormat.RecurrenceMonthlyDay:
            {
                int dayOfMonth = reader.ReadInt32();
                int intervalMonths = reader.ReadInt32();
                DateOnly? anchor = reader.ReadNullableDate();
                var invalidDayBehavior = reader.ReadEnum<Algorithms.InvalidDayOfMonthBehavior>();

                return new MonthlyDayRecurrenceStrategy(dayOfMonth, intervalMonths, anchor, invalidDayBehavior);
            }

            case NotableDateBinaryFormat.RecurrenceMonthlyWeekday:
            {
                var dayOfWeek = reader.ReadEnum<DayOfWeek>();
                var weekOrdinal = reader.ReadEnum<WeekOrdinal>();
                int intervalMonths = reader.ReadInt32();
                DateOnly? anchor = reader.ReadNullableDate();

                return new MonthlyWeekdayRecurrenceStrategy(dayOfWeek, weekOrdinal, intervalMonths, anchor);
            }

            default:
                throw PayloadReader.InvalidValue($"recurrence discriminator {discriminator}");
        }
    }
}
