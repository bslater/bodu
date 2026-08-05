// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResource.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Buffers.Binary;
using System.Security.Cryptography;
using Bodu.Globalization.Calendar.Algorithms;

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Writes and reads the sealed notable-date binary rule-pack format (<c>.bcal</c>): a compact, integrity-checked,
/// reflection-free encoding of a validated <see cref="NotableDateResource" />.
/// </summary>
/// <remarks>
/// <para>
/// A pack is written from an already-built resource, so only content that passed the canonical loader's validation is
/// ever encoded, and reading a pack skips parsing and validation entirely — the trim- and AOT-friendly load path. The
/// format is <em>sealed</em>: it enumerates the engine's strategy, recurrence, and duration types exhaustively with
/// one-byte discriminators, carries no type names and no extension points, and the reader rejects unknown versions,
/// unknown discriminators, undefined enum values, out-of-range references, truncation, and payload-digest mismatches
/// with <see cref="NotableDateBinaryFormatException" />.
/// </para>
/// <para>
/// Writing is byte-stable: the same resource always produces the same bytes, so build systems can use pack outputs for
/// up-to-date checks.
/// </para>
/// </remarks>
/// <seealso cref="NotableDateResourceLoader" /> <seealso cref="NotableDateBinaryFormatException" />
/// <seealso href="../guides/calendar/binary-rule-packs.html">Binary rule packs (guide)</seealso>
public static partial class NotableDateBinaryResource
{
    /// <summary>
    /// Writes a resource to a stream in the binary rule-pack format.
    /// </summary>
    /// <param name="resource">The validated resource to encode.</param>
    /// <param name="stream">The writable stream receiving the pack.</param>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="resource" /> or <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    public static void Write(NotableDateResource resource, Stream stream)
    {
        ThrowHelper.ThrowIfNull(resource);
        ThrowHelper.ThrowIfNull(stream);

        PayloadWriter writer = new();
        WriteResource(writer, resource);
        byte[] payload = writer.ToPayload();

        Span<byte> header = stackalloc byte[NotableDateBinaryFormat.HeaderLength];
        NotableDateBinaryFormat.Magic.CopyTo(header);
        BinaryPrimitives.WriteUInt16LittleEndian(header[4..], NotableDateBinaryFormat.Version);
        BinaryPrimitives.WriteUInt16LittleEndian(header[6..], 0);
        SHA256.HashData(payload, header[8..]);

        stream.Write(header);
        stream.Write(payload);
    }

    /// <summary>
    /// Encodes the resource body: identity, resolution policy, adjustment policies, then concepts.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="resource">The resource to encode.</param>
    private static void WriteResource(PayloadWriter writer, NotableDateResource resource)
    {
        writer.WriteString(resource.ResourceId);
        writer.WriteString(resource.SchemaVersion);

        WriteResolutionPolicy(writer, resource.ResolutionPolicy);

        writer.WriteVarUInt((uint)resource.AdjustmentPolicies.Count);
        foreach (AdjustmentPolicy policy in resource.AdjustmentPolicies)
            WriteAdjustmentPolicy(writer, policy);

        writer.WriteVarUInt((uint)resource.NotableDates.Count);
        foreach (NotableDateDefinition definition in resource.NotableDates)
            WriteDefinition(writer, definition);
    }

    /// <summary>
    /// Encodes the resource-level resolution policy.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="policy">The policy to encode.</param>
    private static void WriteResolutionPolicy(PayloadWriter writer, RangeResolution.ResolutionPolicy policy)
    {
        writer.WriteEnum(policy.DuplicatePolicy);
        writer.WriteEnum(policy.SameDayCollisionPolicy);
        writer.WriteEnum(policy.SpanCollisionPolicy);
        writer.WriteEnum(policy.PriorityDirection);
        writer.WriteEnum(policy.ObservedDateRangePolicy);
        writer.WriteByte(policy.WorkingWeek.ToByte());
        writer.WriteEnumList(policy.CategoryPrecedence);
    }

    /// <summary>
    /// Encodes one adjustment policy with its scope and handler parameters.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="policy">The policy to encode.</param>
    private static void WriteAdjustmentPolicy(PayloadWriter writer, AdjustmentPolicy policy)
    {
        writer.WriteString(policy.Id);
        writer.WriteInt32(policy.Priority);

        writer.WriteStringList(policy.Scope.Territories);
        writer.WriteEnumList(policy.Scope.Calendars);
        writer.WriteEnumList(policy.Scope.Categories);
        writer.WriteStringList(policy.Scope.NotableDateRefs);
        writer.WriteStringList(policy.Scope.RuleRefs);
        writer.WriteNullableInt32(policy.Scope.FromYear);
        writer.WriteNullableInt32(policy.Scope.ToYear);
        writer.WriteInt32List(policy.Scope.OnlyYears);
        writer.WriteInt32List(policy.Scope.ExceptYears);

        writer.WriteEnum(policy.Trigger);
        writer.WriteEnumList(policy.TriggerWeekdays);
        writer.WriteEnum(policy.Action);
        writer.WriteNullableEnum(policy.ActionWeekday);
        writer.WriteInt32(policy.ActionDays);
        writer.WriteNullableInt32(policy.MaxSearchDays);
        writer.WriteBool(policy.SkipWeekends);
        writer.WriteBool(policy.SkipNonWorkingDates);
        writer.WriteEnum(policy.Emission);
        writer.WriteNullableString(policy.Reason);
        writer.WriteBool(policy.NonWorking.HasValue);
        if (policy.NonWorking.HasValue)
            writer.WriteBool(policy.NonWorking.Value);
        writer.WriteNullableInt32(policy.TriggerMonth);
        writer.WriteNullableInt32(policy.TriggerDay);
        writer.WriteNullableEnum(policy.TriggerWeekOrdinal);
        writer.WriteNullableString(policy.ActionNotableDateRef);
        writer.WriteNullableString(policy.ActionRuleRef);
        writer.WriteNullableString(policy.ActionHandlerKey);
        writer.WriteNullableString(policy.TriggerHandlerKey);

        // Handler parameters are written key-sorted so identical dictionaries encode identically regardless of the
        // insertion order they were built with — part of the byte-stability contract.
        writer.WriteVarUInt((uint)policy.HandlerParameters.Count);
        foreach (KeyValuePair<string, string> pair in policy.HandlerParameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            writer.WriteString(pair.Key);
            writer.WriteString(pair.Value);
        }
    }

    /// <summary>
    /// Encodes one notable-date concept with its rules.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="definition">The concept to encode.</param>
    private static void WriteDefinition(PayloadWriter writer, NotableDateDefinition definition)
    {
        writer.WriteString(definition.Id);
        writer.WriteString(definition.DisplayName);
        writer.WriteEnum(definition.Category);
        writer.WriteBool(definition.DefaultNonWorkingDay);
        writer.WriteInt32(definition.DefaultDurationDays);
        writer.WriteStringList(definition.Tags);

        writer.WriteVarUInt((uint)definition.Rules.Count);
        foreach (NotableDateRule rule in definition.Rules)
            WriteRule(writer, rule);
    }

    /// <summary>
    /// Encodes one rule with its applicability, duration, and occurrence source.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="rule">The rule to encode.</param>
    private static void WriteRule(PayloadWriter writer, NotableDateRule rule)
    {
        writer.WriteString(rule.Id);
        writer.WriteInt32(rule.Priority);
        writer.WriteNullableEnum(rule.Category);
        writer.WriteBool(rule.NonWorking.HasValue);
        if (rule.NonWorking.HasValue)
            writer.WriteBool(rule.NonWorking.Value);

        WriteDuration(writer, rule.Duration);

        writer.WriteEnum(rule.Applicability.Calendar);
        writer.WriteNullableInt32(rule.Applicability.FromYear);
        writer.WriteNullableInt32(rule.Applicability.ToYear);
        writer.WriteStringList(rule.Applicability.Territories);
        writer.WriteInt32List(rule.Applicability.OnlyYears);
        writer.WriteInt32List(rule.Applicability.ExceptYears);
        writer.WriteNullableInt32(rule.Applicability.EveryYears);
        writer.WriteNullableInt32(rule.Applicability.AnchorYear);

        if (rule.Strategy is not null)
        {
            writer.WriteByte(NotableDateBinaryFormat.SourceStrategy);
            WriteStrategy(writer, rule.Strategy);
        }
        else
        {
            writer.WriteByte(NotableDateBinaryFormat.SourceRecurrence);
            WriteRecurrence(writer, rule.Recurrence!);
        }

        writer.WriteStringList(rule.AdjustmentPolicyRefs);
        writer.WriteStringList(rule.Tags);
    }

    /// <summary>
    /// Encodes an optional duration override.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="duration">The duration to encode, or <see langword="null" />.</param>
    private static void WriteDuration(PayloadWriter writer, NotableDateDurationDefinition? duration)
    {
        switch (duration)
        {
            case null:
                writer.WriteByte(NotableDateBinaryFormat.DurationNone);
                break;

            case FixedDurationDefinition fixedDuration:
                writer.WriteByte(NotableDateBinaryFormat.DurationFixed);
                writer.WriteInt32(fixedDuration.Days);
                break;

            case CalculatedEndDateDurationDefinition calculated:
                writer.WriteByte(NotableDateBinaryFormat.DurationCalculatedEnd);
                WriteStrategy(writer, calculated.EndStrategy);
                writer.WriteEnum(calculated.StartBoundary);
                writer.WriteEnum(calculated.EndBoundary);
                writer.WriteEnum(calculated.EndSelection);
                break;

            default:
                throw new NotSupportedException(duration.GetType().Name);
        }
    }

    /// <summary>
    /// Encodes a single-date calculation strategy as its discriminator and fields.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="strategy">The strategy to encode.</param>
    private static void WriteStrategy(PayloadWriter writer, IDateCalculationStrategy strategy)
    {
        switch (strategy)
        {
            case FixedDateStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyFixedDate);
                writer.WriteInt32(s.Month);
                writer.WriteInt32(s.Day);
                writer.WriteEnum(s.Calendar);
                writer.WriteBool(s.SweepCalendarYears);
                writer.WriteBool(s.SkipLeapMonth);
                writer.WriteNullableString(s.MonthAlias);
                break;

            case DayOfWeekInMonthStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyDayOfWeekInMonth);
                writer.WriteInt32(s.Month);
                writer.WriteEnum(s.DayOfWeek);
                writer.WriteEnum(s.WeekOrdinal);
                break;

            case RelativeWeekdayInMonthStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyRelativeWeekdayInMonth);
                writer.WriteInt32(s.Month);
                writer.WriteEnum(s.DayOfWeek);
                writer.WriteEnum(s.WeekOrdinal);
                writer.WriteEnum(s.RelativeDayOfWeek);
                writer.WriteEnum(s.Direction);
                break;

            case WeekdayNearDateStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyWeekdayNearDate);
                writer.WriteInt32(s.Month);
                writer.WriteInt32(s.Day);
                writer.WriteEnum(s.DayOfWeek);
                writer.WriteEnum(s.Direction);
                break;

            case WeekdayNearRuleStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyWeekdayNearRule);
                writer.WriteString(s.NotableDateRef);
                writer.WriteNullableString(s.RuleRef);
                writer.WriteEnum(s.DayOfWeek);
                writer.WriteEnum(s.Direction);
                writer.WriteInt32(s.ReferenceYearOffset);
                break;

            case OffsetFromRuleStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyOffsetFromRule);
                writer.WriteString(s.NotableDateRef);
                writer.WriteNullableString(s.RuleRef);
                writer.WriteInt32(s.OffsetDays);
                break;

            case NthWeekdayFromRuleStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyNthWeekdayFromRule);
                writer.WriteString(s.NotableDateRef);
                writer.WriteNullableString(s.RuleRef);
                writer.WriteEnum(s.DayOfWeek);
                writer.WriteInt32(s.Ordinal);
                writer.WriteInt32(s.ReferenceYearOffset);
                break;

            case WorkingDayOffsetFromRuleStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyWorkingDayOffsetFromRule);
                writer.WriteString(s.NotableDateRef);
                writer.WriteNullableString(s.RuleRef);
                writer.WriteInt32(s.OffsetWorkingDays);
                writer.WriteInt32(s.ReferenceYearOffset);
                break;

            case WorkingDayInMonthStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyWorkingDayInMonth);
                writer.WriteInt32(s.Month);
                writer.WriteInt32(s.Ordinal);
                break;

            case OrdinalDayOfMonthStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyOrdinalDayOfMonth);
                writer.WriteInt32(s.Month);
                writer.WriteInt32(s.Ordinal);
                break;

            case IsoWeekDateStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyIsoWeekDate);
                writer.WriteInt32(s.Week);
                writer.WriteEnum(s.DayOfWeek);
                break;

            case DayOfYearStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyDayOfYear);
                writer.WriteInt32(s.Ordinal);
                break;

            case AlgorithmDateStrategy s:
                writer.WriteByte(NotableDateBinaryFormat.StrategyAlgorithm);
                writer.WriteString(s.Key);
                break;

            default:
                throw new NotSupportedException(strategy.GetType().Name);
        }
    }

    /// <summary>
    /// Encodes a recurrence strategy as its discriminator and fields.
    /// </summary>
    /// <param name="writer">The payload writer.</param>
    /// <param name="recurrence">The recurrence to encode.</param>
    private static void WriteRecurrence(PayloadWriter writer, IDateRecurrenceStrategy recurrence)
    {
        switch (recurrence)
        {
            case DailyIntervalRecurrenceStrategy r:
                writer.WriteByte(NotableDateBinaryFormat.RecurrenceDailyInterval);
                writer.WriteDate(r.AnchorDate);
                writer.WriteInt32(r.IntervalDays);
                break;

            case WeeklyRecurrenceStrategy r:
                writer.WriteByte(NotableDateBinaryFormat.RecurrenceWeekly);
                writer.WriteEnumList(r.DaysOfWeek);
                writer.WriteInt32(r.IntervalWeeks);
                writer.WriteNullableDate(r.AnchorDate);
                break;

            case MonthlyDayRecurrenceStrategy r:
                writer.WriteByte(NotableDateBinaryFormat.RecurrenceMonthlyDay);
                writer.WriteInt32(r.DayOfMonth);
                writer.WriteInt32(r.IntervalMonths);
                writer.WriteNullableDate(r.AnchorDate);
                writer.WriteEnum(r.InvalidDayBehavior);
                break;

            case MonthlyWeekdayRecurrenceStrategy r:
                writer.WriteByte(NotableDateBinaryFormat.RecurrenceMonthlyWeekday);
                writer.WriteEnum(r.DayOfWeek);
                writer.WriteEnum(r.WeekOrdinal);
                writer.WriteInt32(r.IntervalMonths);
                writer.WriteNullableDate(r.AnchorDate);
                break;

            default:
                throw new NotSupportedException(recurrence.GetType().Name);
        }
    }
}
