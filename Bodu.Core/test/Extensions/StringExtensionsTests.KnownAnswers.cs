// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StringExtensionsTests.KnownAnswers.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Tests.KnownAnswers;

namespace Bodu.Extensions;

public partial class StringExtensionsTests
{
    /// <summary>
    /// Verifies that <see cref="StringExtensions" /> casing and formatting methods satisfy every scenario in
    /// <see cref="StringFormattingKnownAnswers" />.
    /// </summary>
    /// <param name="kat">The known-answer scenario to evaluate.</param>
    /// <remarks>
    /// Each scenario is dispatched to the acronym-aware rich overloads. The <see cref="WordCasingOptions" />
    /// and <see cref="SlugOptions" /> instances are built directly from the scenario so the formatter is
    /// driven by the exact acronym list, minor-word list, culture, and preservation flags the scenario
    /// declares.
    /// </remarks>
    [TestMethod]
    [DynamicData(
        nameof(StringFormattingKnownAnswers.All),
        typeof(StringFormattingKnownAnswers),
        DynamicDataDisplayName = nameof(StringFormattingKnownAnswers.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(StringFormattingKnownAnswers))]
    public void StringFormatting_AgainstKnownAnswer_ShouldMatchExpected(StringFormattingKnownAnswer kat)
    {
        CultureInfo culture = kat.CultureName is null
            ? CultureInfo.InvariantCulture
            : CultureInfo.GetCultureInfo(kat.CultureName);

        WordCasingOptions casing = new()
        {
            Acronyms = kat.Acronyms ?? Array.Empty<string>(),
            MinorWords = kat.MinorWords ?? Array.Empty<string>(),
            Culture = culture,
            PreserveAcronyms = kat.PreserveExistingAcronyms,
            PreserveMixedCaseWords = kat.PreserveMixedCaseWords,
            LowerCaseMinorWords = kat.Operation == StringFormattingOperation.TitleCase,
        };

        SlugOptions slug = new()
        {
            NormalizeDiacritics = kat.NormalizeDiacritics,
        };

        var actual = kat.Operation switch
        {
            StringFormattingOperation.TitleCase => kat.Input.ToTitleCase(casing),
            StringFormattingOperation.SentenceCase => kat.Input.ToSentenceCase(casing),
            StringFormattingOperation.CamelCase => kat.Input.ToCamelCase(casing),
            StringFormattingOperation.PascalCase => kat.Input.ToPascalCase(casing),
            StringFormattingOperation.SnakeCase => kat.Input.ToSnakeCase(casing),
            StringFormattingOperation.KebabCase => kat.Input.ToKebabCase(casing),
            StringFormattingOperation.TrainCase => kat.Input.ToTrainCase(casing),
            StringFormattingOperation.ConstantCase => kat.Input.ToConstantCase(casing),
            StringFormattingOperation.DotCase => kat.Input.ToDotCase(casing),
            StringFormattingOperation.Slug => kat.Input.ToSlug(slug),
            StringFormattingOperation.NormalizeWhitespace => kat.Input.CollapseWhitespace().Trim(),
            _ => throw new ArgumentOutOfRangeException(nameof(kat), $"Unknown operation {kat.Operation}."),
        };

        Assert.AreEqual(kat.Expected, actual, $"[{kat.Id}] {kat.Scenario}");
    }
}
