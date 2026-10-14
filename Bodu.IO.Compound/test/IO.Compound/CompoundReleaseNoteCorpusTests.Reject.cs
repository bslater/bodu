// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that opening each cataloged container a fix established as malformed, or reading one of its streams,
    /// throws <see cref="CompoundFileFormatException" />, with the <see cref="CompoundFileError" /> category the row
    /// names when it names one.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowCompoundFileFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] container = ReadContainer(fix.Input);
        CompoundFileError? category = ReadExpectedCategory(fix);

        CompoundFileFormatException ex = Assert.ThrowsExactly<CompoundFileFormatException>(
            () =>
            {
                _ = OpenAndRender(container, options);
            },
            fix.ToString());

        if (category is { } expected)
            Assert.AreEqual(expected, ex.Category, $"{fix}: threw {Describe(ex)}");
    }

    /// <summary>
    /// Reads the exception a rejecting row expects: <see cref="CompoundFileFormatException" />, optionally followed by
    /// <c>:</c> and a <see cref="CompoundFileError" /> member.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <returns>The category the row names, or <see langword="null" /> when it names none.</returns>
    /// <exception cref="FormatException">
    /// The row names another exception, or a category that is not a member.
    /// </exception>
    private static CompoundFileError? ReadExpectedCategory(ReleaseNoteFix fix)
    {
        int colon = fix.Expected.IndexOf(':', StringComparison.Ordinal);
        string typeName = colon < 0 ? fix.Expected : fix.Expected[..colon];
        if (typeName != nameof(CompoundFileFormatException))
            throw new FormatException($"The row expects {typeName}; a reject row expects {nameof(CompoundFileFormatException)}.");

        if (colon < 0)
            return null;

        string category = fix.Expected[(colon + 1)..];
        return Enum.GetNames<CompoundFileError>().Contains(category, StringComparer.Ordinal)
            ? Enum.Parse<CompoundFileError>(category)
            : throw new FormatException($"The category '{category}' is not a {nameof(CompoundFileError)} member.");
    }
}
