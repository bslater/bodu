// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CustomAlgorithmTests.BuiltInAlgorithm.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

public sealed partial class CustomAlgorithmTests
{
    /// <summary>
    /// Verifies that the built-in <c>western-easter</c> algorithm resolves without any custom registry, confirming the
    /// built-in catalogue is available through the default registry.
    /// </summary>
    [TestMethod]
    public void BuiltInAlgorithm_WhenNoCustomRegistry_ResolvesFromDefaultCatalogue()
    {
        NotableDateResource resource = NotableDateResourceLoader.Load(EasterXml);
        NotableDateService service = new(resource);

        NotableDate match = service
            .Resolve(new DateRange(new DateOnly(2024, 1, 1), new DateOnly(2024, 12, 31)), "XX")
            .Single(r => r.NotableDateId == "easter");

        Assert.AreEqual(new DateOnly(2024, 3, 31), match.Date);
    }
}
