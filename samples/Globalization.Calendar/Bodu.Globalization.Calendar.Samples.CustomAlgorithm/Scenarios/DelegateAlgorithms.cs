// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DelegateAlgorithms.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;
using Bodu.Globalization.Calendar.Algorithms;
using Bodu.Globalization.Calendar.Builder;

namespace Bodu.Globalization.Calendar.Samples.CustomAlgorithm.Scenarios;

/// <summary>
/// Demonstrates a lambda-backed algorithm: <see cref="INotableDateAlgorithm" /> is a single method,
/// so a five-line adapter turns any <c>Func&lt;int, DateOnly?&gt;</c> into a registrable algorithm —
/// the right shape when the mathematics is a couple of lines and will not be reused elsewhere.
/// </summary>
public static class DelegateAlgorithms
{
    /// <summary>
    /// Registers a lambda-based algorithm (last Friday of June) and resolves it.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Delegate algorithms: a lambda as the calculation ---");

        // Last Friday of June - end-of-financial-year party (AU). Walk back from 30 June.
        var algorithms = new NotableDateAlgorithmRegistry()
            .Register("eofy-party", new LambdaAlgorithm(year =>
            {
                var eofy = new DateOnly(year, 6, 30);
                var back = ((int)eofy.DayOfWeek - (int)DayOfWeek.Friday + 7) % 7;
                return eofy.AddDays(-back);
            }));

        var xml = NotableDateDocumentBuilder.Create("contoso-eofy")
            .AddNotableDate("eofy-party", "EOFY Party", NotableDateCategory.Cultural, c => c
                .AddRule("algorithm", r => r.Algorithm("eofy-party")))
            .ToXml();

        NotableDateResource resource = NotableDateResourceLoader.Load(xml, _ => null, algorithms);
        var service = new NotableDateService(resource, new NotableDateServiceOptions { Algorithms = algorithms });

        foreach (var year in new[] { 2024, 2025 })
        {
            NotableDate party = service.Resolve(year, "AU").Single();
            Console.WriteLine($"  {year}: {party.Date:yyyy-MM-dd} ({party.Date.DayOfWeek}) {party.DisplayName}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// The whole adapter: an <see cref="INotableDateAlgorithm" /> is one method, so wrapping a
    /// delegate takes five lines. Prefer a named class (like <c>CompanyFoundingDayAlgorithm</c>)
    /// once the calculation deserves its own tests.
    /// </summary>
    private sealed class LambdaAlgorithm : INotableDateAlgorithm
    {
        private readonly Func<int, DateOnly?> _calculate;

        /// <summary>Initializes the adapter over <paramref name="calculate" />.</summary>
        /// <param name="calculate">The per-year calculation.</param>
        public LambdaAlgorithm(Func<int, DateOnly?> calculate) => _calculate = calculate;

        /// <inheritdoc />
        public DateOnly? Calculate(int year) => _calculate(year);
    }
}
