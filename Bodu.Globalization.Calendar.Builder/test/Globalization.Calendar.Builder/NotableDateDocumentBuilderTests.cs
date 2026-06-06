// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Extensions;

namespace Bodu.Globalization.Calendar.Builder;

/// <summary>
/// Contains unit tests for <see cref="NotableDateDocumentBuilder" />. The partial class is split across files by the
/// behaviour under test (build, serialization, round-trip, save/load, clone).
/// </summary>
[TestClass]
public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Builds a small but representative document exercising metadata, a resolution policy, a reusable adjustment
    /// policy, and notable dates spanning the fixed, day-of-week-in-month, offset-from-rule, and algorithm strategies.
    /// </summary>
    /// <returns>A configured <see cref="NotableDateDocumentBuilder" /> ready to build or serialize.</returns>
    private static NotableDateDocumentBuilder SampleDocument() =>
        NotableDateDocumentBuilder.Create("demo.sample")
            .WithMetadata(name: "Sample", description: "A sample document.", "unit-test")
            .WithResolutionPolicy(p => p
                .WithDuplicatePolicy(RangeResolution.DuplicatePolicy.KeepFirst)
                .WithWorkingWeek(WeekPattern.MondayToFriday))
            .AddAdjustmentPolicy("weekend-to-monday", a => a
                .WithDescription("Observe weekend holidays on the next working day.")
                .When(AdjustmentTrigger.IfWeekend)
                .Then(AdjustmentAction.MoveToNextWorkingDay)
                .Emit(RangeResolution.EmissionMode.ObservedOnly))
            .AddNotableDate("new-years-day", "New Year's Day", NotableDateCategory.PublicHoliday, d => d
                .AsNonWorkingByDefault()
                .AddRule("default", r => r
                    .ForTerritory("US")
                    .Fixed(1, 1)
                    .WithAdjustment("weekend-to-monday")))
            .AddNotableDate("thanksgiving", "Thanksgiving Day", NotableDateCategory.PublicHoliday, d => d
                .AsNonWorkingByDefault()
                .AddRule("default", r => r
                    .ForTerritory("US")
                    .DayOfWeekInMonth(11, DayOfWeek.Thursday, WeekOrdinal.Fourth)))
            .AddNotableDate("easter-sunday", "Easter Sunday", NotableDateCategory.Religious, d => d
                .AddRule("default", r => r.Algorithm("western-easter")))
            .AddNotableDate("good-friday", "Good Friday", NotableDateCategory.PublicHoliday, d => d
                .AsNonWorkingByDefault()
                .AddRule("default", r => r.OffsetFromRule("easter-sunday", -2, "default")));

    /// <summary>
    /// Builds the supplied builder into a resource and wraps it in a <see cref="NotableDateService" /> for end-to-end
    /// resolution assertions.
    /// </summary>
    /// <param name="builder">The document builder to materialize.</param>
    /// <returns>A service over the built resource.</returns>
    private static NotableDateService ServiceFor(NotableDateDocumentBuilder builder) =>
        new(builder.Build());
}
