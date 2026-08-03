// ---------------------------------------------------------------------------------------------------------------
// <copyright file="HinduReferenceCorpusReconciliationTests.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar;

/// <summary>
/// Reconciles the engine's <c>global-hindu</c> festival dates against the calcal Modern Hindu Reference corpus
/// (an independent executable implementation of the Reingold-Dershowitz algorithms; see <c>corpus/README.md</c>).
/// </summary>
/// <remarks>
/// <para>
/// The corpus file is generated user-side by the pinned R oracle and committed under
/// <c>corpus/hindu/data/normalized/</c>; until it exists every test here reports <c>Inconclusive</c>. Bodu's
/// calculator and the oracle are different model families (Meeus sidereal/tithi versus Reingold-Dershowitz), so
/// comparisons are classified rather than forced to exactness: <c>exact</c>, <c>convention-equivalent</c> (one day,
/// the documented panchanga/day-assignment band), or beyond the band, which fails the sweep with the full class
/// distribution in the message.
/// </para>
/// </remarks>
[TestClass]
public sealed class HinduReferenceCorpusReconciliationTests
{
    /// <summary>The reference-corpus message shown while the user-side hand-back is outstanding.</summary>
    private const string CorpusAbsentMessage =
        "The calcal reference corpus has not been generated yet - run corpus/hindu/reference/calcal/generate_corpus.R "
        + "(see its README) and commit corpus/hindu/data/normalized/hindu-reference-daily.csv.";

    /// <summary>
    /// The lunar festival coordinates in reference-corpus terms: the amanta month and the lunar day (tithi ordinal
    /// 1-30; shukla T = day T, krishna T = day 15 + T, purnima = day 15). Diwali is the Ashvina amavasya (month 7
    /// day 30): expressing it as the following month's first day breaks in an adhika-Kartika year (2028), where the
    /// festival stays with the Ashvina-ending new moon while the corpus defers non-leap Kartika a full lunation.
    /// </summary>
    private static readonly (string Id, int Month, int Day)[] s_lunarFestivals =
    [
        ("ram-navami", 1, 9),
        ("raksha-bandhan", 5, 15),
        ("janmashtami", 5, 23),
        ("ganesh-chaturthi", 6, 4),
        ("navaratri", 7, 1),
        ("dussehra", 7, 10),
        ("karva-chauth", 7, 19),
        ("diwali", 7, 30),
        ("vasant-panchami", 11, 5),
        ("saraswati-puja", 11, 5),
        ("maha-shivaratri", 11, 29),
        ("holi", 12, 15),
    ];

    /// <summary>The shared engine service over the bundled catalogue.</summary>
    private static readonly Lazy<NotableDateService> s_service = new(() => CommonCatalogues.Service("global-hindu"));

    /// <summary>
    /// Verifies that the reference corpus loads under the strict contract (exact dates, no duplicate keys, gap-free
    /// per-model coverage) when it is present.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Load_WhenCorpusPresent_ShouldSatisfyStrictContract()
    {
        string? path = HinduReferenceCorpus.FindCorpusFile();
        if (path is null)
        {
            Assert.Inconclusive(CorpusAbsentMessage);
            return;
        }

        SortedDictionary<DateOnly, HinduReferenceCorpusRow> lunar = HinduReferenceCorpus.Load(path, "modern-hindu-lunar");
        SortedDictionary<DateOnly, HinduReferenceCorpusRow> solar = HinduReferenceCorpus.Load(path, "modern-hindu-solar");

        Assert.IsNotEmpty(lunar, "the modern-hindu-lunar model has no rows");
        Assert.IsNotEmpty(solar, "the modern-hindu-solar model has no rows");
        Assert.AreEqual(lunar.Count, solar.Count, "the two modern models cover different ranges");
    }

    /// <summary>
    /// Verifies that every engine-resolved lunar festival lies within two days of the date the reference corpus
    /// implies for the festival's amanta month and lunar day, across every year both sources cover, reporting the
    /// comparison-class distribution.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The classes follow the corpus policy (<c>corpus/README.md</c>, lineage warning): <c>exact</c>;
    /// <c>convention-equivalent</c> (one day — panchanga day-assignment); and <c>model-induced</c> (two days — the
    /// documented bound of the engine's Meeus/Lahiri approximations against the Reingold-Dershowitz family, the same
    /// ±1–2 day band the engine-pinned vector table has carried since inception). Anything beyond two days, or a
    /// festival whose target month cannot be found near the engine date at all (a wrong-lunation selection), fails
    /// the sweep — that is the class the corpus caught in 1991/2009/2010 before the ingress-instant fix.
    /// </para>
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    public void Resolve_WhenComparedWithReferenceCorpus_ShouldStayWithinConventionBand()
    {
        string? path = HinduReferenceCorpus.FindCorpusFile();
        if (path is null)
        {
            Assert.Inconclusive(CorpusAbsentMessage);
            return;
        }

        SortedDictionary<DateOnly, HinduReferenceCorpusRow> corpus = HinduReferenceCorpus.Load(path, "modern-hindu-lunar");
        int firstYear = Math.Max(1990, corpus.Keys.Min().Year + 1);
        int lastYear = Math.Min(2039, corpus.Keys.Max().Year - 1);

        int exact = 0, conventionEquivalent = 0, modelInduced = 0;
        List<string> beyondBand = new();

        for (int year = firstYear; year <= lastYear; year++)
        {
            foreach ((string id, int month, int day) in s_lunarFestivals)
            {
                List<NotableDate> resolved = CommonCatalogues.ResolveForYear(s_service.Value, id, year);
                Assert.HasCount(1, resolved, $"expected exactly one '{id}' in {year}");

                DateOnly? implied = ImpliedDate(corpus, resolved[0].Date, month, day);
                Assert.IsNotNull(implied, $"{id} {year}: no corpus date for amanta month {month} day {day} near {resolved[0].Date:yyyy-MM-dd}");

                int distance = Math.Abs(resolved[0].Date.DayNumber - implied.Value.DayNumber);
                if (distance == 0)
                    exact++;
                else if (distance == 1)
                    conventionEquivalent++;
                else if (distance == 2)
                    modelInduced++;
                else
                    beyondBand.Add($"{id} {year}: engine {resolved[0].Date:yyyy-MM-dd} vs corpus {implied:yyyy-MM-dd} ({distance} d)");
            }
        }

        Assert.IsEmpty(
            beyondBand,
            $"comparisons beyond the two-day band (exact {exact}, convention-equivalent {conventionEquivalent}, "
            + $"model-induced {modelInduced}):{Environment.NewLine}"
            + string.Join(Environment.NewLine, beyondBand));
    }

    /// <summary>
    /// Verifies that the reference corpus places the start of solar month 10 (the Makara ingress that fixes Makar
    /// Sankranti and Pongal) within one day of 14 January for every year the corpus covers.
    /// </summary>
    [TestMethod]
    [TestCategory("Regression")]
    public void Load_WhenSolarModelPresent_ShouldPlaceMakaraIngressNear14January()
    {
        string? path = HinduReferenceCorpus.FindCorpusFile();
        if (path is null)
        {
            Assert.Inconclusive(CorpusAbsentMessage);
            return;
        }

        SortedDictionary<DateOnly, HinduReferenceCorpusRow> solar = HinduReferenceCorpus.Load(path, "modern-hindu-solar");
        List<string> outliers = new();

        foreach (HinduReferenceCorpusRow row in solar.Values)
        {
            if (row.SolarMonth != 10 || row.SolarDay != 1)
                continue;

            var civil = new DateOnly(row.GregorianDate.Year, 1, 14);
            int distance = Math.Abs(row.GregorianDate.DayNumber - civil.DayNumber);
            if (distance > 1)
                outliers.Add($"{row.GregorianDate:yyyy-MM-dd} is {distance} days from 14 January");
        }

        Assert.IsEmpty(outliers, string.Join(Environment.NewLine, outliers));
    }

    /// <summary>
    /// Locates the corpus date implied by an amanta (month, lunar day) coordinate nearest the engine's resolved
    /// date, treating a kshaya (skipped) tithi as the first civil day whose lunar day passes the target.
    /// </summary>
    /// <param name="corpus">The modern-hindu-lunar rows keyed by date.</param>
    /// <param name="near">The engine-resolved date anchoring the search window.</param>
    /// <param name="month">The one-based amanta month.</param>
    /// <param name="day">The target lunar day (tithi ordinal 1-30).</param>
    /// <returns>The implied civil date, or <see langword="null" /> when the window holds no matching month.</returns>
    private static DateOnly? ImpliedDate(SortedDictionary<DateOnly, HinduReferenceCorpusRow> corpus, DateOnly near, int month, int day)
    {
        DateOnly? kshayaCandidate = null;

        // A 45-day window centered on the engine date always contains the full target month.
        for (int offset = -22; offset <= 22; offset++)
        {
            DateOnly date = near.AddDays(offset);
            if (!corpus.TryGetValue(date, out HinduReferenceCorpusRow? row) || row.LunarMonth != month || row.IsLeapMonth == true)
                continue;

            if (row.LunarDay == day)
                return date;

            // Kshaya handling: remember the first day of the month whose lunar day has passed the target.
            if (row.LunarDay > day
                && (kshayaCandidate is null || date < kshayaCandidate)
                && (!corpus.TryGetValue(date.AddDays(-1), out HinduReferenceCorpusRow? previous)
                    || previous.LunarMonth != month
                    || previous.LunarDay < day))
            {
                kshayaCandidate = date;
            }
        }

        return kshayaCandidate;
    }
}
