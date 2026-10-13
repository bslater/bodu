// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateBinaryResourceTests.Write.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.RangeResolution;

namespace Bodu.Globalization.Calendar;

public sealed partial class NotableDateBinaryResourceTests
{
    /// <summary>The applicability of the single rule the write tests encode.</summary>
    private static readonly RuleApplicability GlobalApplicability = new(CalendarSystem.Gregorian, null, null, [], [], []);

    /// <summary>
    /// Verifies that writing a rule whose calculation strategy is outside the sealed set throws
    /// <see cref="NotSupportedException" /> with the package's resource message naming the strategy type.
    /// </summary>
    [TestMethod]
    public void Write_WhenStrategyIsCustom_ShouldThrowNotSupportedExceptionFromResource()
    {
        NotableDateResource resource = SingleRuleResource(
            new NotableDateRule("r", 0, null, null, null, GlobalApplicability, new CustomStrategy(), [], []));

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.AreEqual(UnsupportedTypeMessage(nameof(CustomStrategy)), ex.Message);
    }

    /// <summary>
    /// Verifies that writing a rule whose recurrence strategy is outside the sealed set throws
    /// <see cref="NotSupportedException" /> with the package's resource message naming the recurrence type.
    /// </summary>
    [TestMethod]
    public void Write_WhenRecurrenceIsCustom_ShouldThrowNotSupportedExceptionFromResource()
    {
        NotableDateResource resource = SingleRuleResource(
            new NotableDateRule("r", 0, null, null, null, GlobalApplicability, new CustomRecurrence(), [], []));

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.AreEqual(UnsupportedTypeMessage(nameof(CustomRecurrence)), ex.Message);
    }

    /// <summary>
    /// Verifies that writing a rule whose duration definition is outside the sealed set throws
    /// <see cref="NotSupportedException" /> with the package's resource message naming the duration type.
    /// </summary>
    [TestMethod]
    public void Write_WhenDurationIsCustom_ShouldThrowNotSupportedExceptionFromResource()
    {
        NotableDateResource resource = SingleRuleResource(
            new NotableDateRule("r", 0, null, null, new CustomDuration(), GlobalApplicability, new FixedDateStrategy(1, 1), [], []));

        NotSupportedException ex = Assert.ThrowsExactly<NotSupportedException>(() =>
        {
            _ = WritePack(resource);
        });

        Assert.AreEqual(UnsupportedTypeMessage(nameof(CustomDuration)), ex.Message);
    }

    /// <summary>
    /// Builds a resource holding one concept with the supplied rule.
    /// </summary>
    /// <param name="rule">The concept's only rule.</param>
    /// <returns>The resource.</returns>
    private static NotableDateResource SingleRuleResource(NotableDateRule rule) =>
        new(
            "test.write",
            "1.0",
            ResolutionPolicy.Default,
            [],
            [new NotableDateDefinition("custom", "Custom", NotableDateCategory.Observance, false, 1, [], [rule])]);

    /// <summary>
    /// Formats the package's message for a type the binary format cannot encode, read from its resources.
    /// </summary>
    /// <param name="typeName">The name of the unsupported type.</param>
    /// <returns>The expected message, or <see langword="null" /> when the resource is absent.</returns>
    private static string? UnsupportedTypeMessage(string typeName)
    {
        string? format = CalendarResourceStrings.ResourceManager.GetString("Op_NotSupported_BinaryPackType", CultureInfo.CurrentCulture);

        return format is null ? null : string.Format(CultureInfo.CurrentCulture, format, typeName);
    }

    /// <summary>A calculation strategy outside the sealed set the binary format enumerates.</summary>
    private sealed class CustomStrategy
        : IDateCalculationStrategy
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year, StrategyResolutionContext context) =>
            new DateOnly(year, 1, 1);
    }

    /// <summary>A recurrence strategy outside the sealed set the binary format enumerates.</summary>
    private sealed class CustomRecurrence
        : IDateRecurrenceStrategy
    {
        /// <inheritdoc />
        public IEnumerable<DateOnly> GetOccurrences(DateRange range, StrategyResolutionContext context) =>
            [range.StartDate];
    }

    /// <summary>A duration definition outside the sealed set the binary format enumerates.</summary>
    private sealed record CustomDuration
        : NotableDateDurationDefinition;
}
