// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAlgorithmTests.CustomAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class CustomAlgorithmTests
{
    /// <summary>
    /// Verifies that a registered custom algorithm validates and resolves to its computed date.
    /// </summary>
    [TestMethod]
    public void CustomAlgorithm_WhenRegistered_ValidatesAndResolves()
    {
        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry().Register("pi-day", new PiDayAlgorithm());

        NotableDateResource resource = NotableDateResourceLoader.Load(Xml, _ => null, registry);
        NotableDateService service = new(resource, new NotableDateServiceOptions { Algorithms = registry });

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

        Assert.Contains(d => d.Code == "BODU-CAL-ALGORITHM", ex.Diagnostics);
    }

    /// <summary>
    /// Verifies that a custom algorithm registered under a built-in key takes precedence over the built-in computation.
    /// </summary>
    [TestMethod]
    public void CustomAlgorithm_WhenRegisteredUnderBuiltInKey_OverridesBuiltIn()
    {
        NotableDateAlgorithmRegistry registry = new NotableDateAlgorithmRegistry().Register("western-easter", new AprilFoolsAlgorithm());

        NotableDateResource resource = NotableDateResourceLoader.Load(EasterXml, _ => null, registry);
        NotableDateService service = new(resource, new NotableDateServiceOptions { Algorithms = registry });

        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Single(r => r.NotableDateId == "easter");

        Assert.AreEqual(new DateOnly(2024, 4, 1), match.Date);
    }
}
