// ---------------------------------------------------------------------------------------------------------------
// <copyright file="IniReleaseNoteCorpusTests.Reject.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.Text.Ini;

public sealed partial class IniReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued input a fix established as malformed is rejected by the view the row selects with
    /// <see cref="IniFormatException" />, the exception the row names.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RejectRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void Reject_WhenFixScenarioIsRead_ShouldThrowIniFormatException(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        byte[] input = CorpusEscapes.Decode(fix.Input);
        Assert.AreEqual(nameof(IniFormatException), fix.Expected, $"{fix}: a read is rejected with IniFormatException.");
        string rendering = string.Empty;

        Assert.ThrowsExactly<IniFormatException>(
            () =>
            {
                rendering = Render(input, options);
            },
            thrown => thrown is null ? $"{fix}: no exception; rendered {rendering}." : fix.ToString());
    }
}
