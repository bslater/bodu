// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAlgorithmTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.V2;

/// <summary>
/// Verifies that a custom <see cref="INotableDateAlgorithm" /> registered with a
/// <see cref="NotableDateAlgorithmRegistry" /> is accepted during validation and dispatched during resolution.
/// </summary>
[TestClass]
public sealed class CustomAlgorithmTests
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
    /// A custom algorithm placing the occurrence on 14 March.
    /// </summary>
    private sealed class PiDayAlgorithm : INotableDateAlgorithm
    {
        /// <inheritdoc />
        public DateOnly? Calculate(int year) =>
            new DateOnly(year, 3, 14);
    }

    /// <summary>
    /// Verifies that a registered custom algorithm validates and resolves to its computed date.
    /// </summary>
    [TestMethod]
    public void CustomAlgorithm_WhenRegistered_ValidatesAndResolves()
    {
        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry().Register("pi-day", new PiDayAlgorithm());

        NotableDateResource resource = NotableDateResourceLoader.Load(Xml, _ => null, registry);
        NotableDateService service = new(resource, registry);

        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Single(r => r.NotableDateId == "pi-day");

        Assert.AreEqual(new DateOnly(2024, 3, 14), match.Date);
    }

    /// <summary>
    /// Verifies that an unregistered custom algorithm key fails validation.
    /// </summary>
    [TestMethod]
    public void CustomAlgorithm_WhenNotRegistered_FailsValidation()
    {
        NotableDateValidationException ex = Assert.ThrowsExactly<NotableDateValidationException>(() =>
        {
            _ = NotableDateResourceLoader.Load(Xml);
        });

        Assert.IsTrue(ex.Diagnostics.Any(d => d.Code == "BODU-CAL2-ALGORITHM"));
    }
}
