// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.WriteReject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>The exceptions a <c>write-reject</c> row may expect, each mapped to its exact-type assertion.</summary>
    private static readonly Dictionary<string, Func<Action, string, Exception>> s_writeRejections = new(StringComparer.Ordinal)
    {
        [nameof(ArgumentException)] = (action, message) => Assert.ThrowsExactly<ArgumentException>(action, message),
        [nameof(KeyNotFoundException)] = (action, message) => Assert.ThrowsExactly<KeyNotFoundException>(action, message),
        [nameof(CompoundFileFormatException)] = (action, message) => Assert.ThrowsExactly<CompoundFileFormatException>(action, message),
        [nameof(CompoundFileSerializationException)] = (action, message) => Assert.ThrowsExactly<CompoundFileSerializationException>(action, message),
        [nameof(CompoundStreamNotFoundException)] = (action, message) => Assert.ThrowsExactly<CompoundStreamNotFoundException>(action, message),
    };

    /// <summary>
    /// Verifies that running each cataloged edit script a fix established as refused, or committing it, throws exactly
    /// the exception the row names.
    /// </summary>
    /// <param name="fix">The catalog row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(WriteRejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void WriteReject_WhenFixScenarioIsWritten_ShouldThrowTheExpectedException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        string input = CorpusEscapes.DecodeText(fix.Input);
        Func<Action, string, Exception> assertThrows = ReadWriteRejection(fix);

        _ = assertThrows(
            () =>
            {
                _ = RunEdits(input);
            },
            fix.ToString());
    }

    /// <summary>
    /// Selects the exact-type assertion for the exception a <c>write-reject</c> row expects.
    /// </summary>
    /// <param name="fix">The row.</param>
    /// <returns>The assertion, which runs an action and returns the exception it threw.</returns>
    /// <exception cref="FormatException">The row expects an exception this suite does not assert.</exception>
    private static Func<Action, string, Exception> ReadWriteRejection(ReleaseNoteFix fix) =>
        s_writeRejections.TryGetValue(fix.Expected, out Func<Action, string, Exception>? assertThrows)
            ? assertThrows
            : throw new FormatException($"The row expects {fix.Expected}; a write-reject row expects one of {string.Join(", ", s_writeRejections.Keys)}.");
}
