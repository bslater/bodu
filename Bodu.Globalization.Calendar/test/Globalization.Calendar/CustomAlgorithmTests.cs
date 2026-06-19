// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Verifies that a custom <see cref="INotableDateAlgorithm" /> registered with a
/// <see cref="NotableDateAlgorithmRegistry" /> is accepted during validation and dispatched during resolution.
/// </summary>
[TestClass]
public sealed partial class CustomAlgorithmTests
{
    /// <summary>
    /// A resource referencing a custom algorithm key.
    /// </summary>
    private const string Xml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.custom">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="pi-day" displayName="Pi Day" category="Observance" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Algorithm key="pi-day" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A resource referencing the built-in <c>western-easter</c> algorithm key.
    /// </summary>
    private const string EasterXml = """
    <NotableDateResource xmlns="urn:bodu:globalization:calendar" schemaVersion="1.0" resourceId="data.easter">
      <ResolutionPolicy duplicatePolicy="Error" priorityDirection="HigherWins" />
      <NotableDates>
        <NotableDate id="easter" displayName="Easter" category="Religious" defaultNonWorkingDay="false">
          <Rules><Rule id="x"><Strategy><Algorithm key="western-easter" /></Strategy></Rule></Rules>
        </NotableDate>
      </NotableDates>
    </NotableDateResource>
    """;

    /// <summary>
    /// A custom algorithm placing the occurrence on 14 March.
    /// </summary>
    private sealed class PiDayAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 3, 14);
    }

    /// <summary>
    /// A custom algorithm placing every occurrence on 1 April, used to override a built-in key.
    /// </summary>
    private sealed class AprilFoolsAlgorithm
        : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 4, 1);
    }

}
