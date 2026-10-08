// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConfigurationReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Configuration;

public sealed partial class ConfigurationReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a row expects Bodu to refuse is rejected, while it is parsed under the row's
    /// profile or resolved for the row's target path, with <see cref="ConfigurationParseException" />, the exception
    /// the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsResolved_ShouldThrowConfigurationParseException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        var options = RowOptions.Parse(fix.Options);
        string text = CorpusEscapes.DecodeText(fix.Input);
        Assert.AreEqual(
            nameof(ConfigurationParseException),
            fix.Expected,
            $"{fix}: a read is rejected with ConfigurationParseException.");

        Assert.ThrowsExactly<ConfigurationParseException>(
            () =>
            {
                _ = ReadAndResolve(text, options);
            },
            fix.ToString());
    }
}
