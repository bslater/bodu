// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CompoundReleaseNoteCorpusTests.RoundTrip.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound.Builders;
using Bodu.Test.Corpus;
using Bodu.Test.Kat;

namespace Bodu.IO.Compound;

public sealed partial class CompoundReleaseNoteCorpusTests
{
    /// <summary>
    /// Verifies that each catalogued container, or the result of each catalogued edit script, reads, and that copying
    /// it through <see cref="CompoundStorageBuilder.FromFile(CompoundFile, bool)" /> and
    /// <see cref="CompoundStorageBuilder.ToArray(CompoundBuildOptions)" /> reads back to the same listing, which equals
    /// the expected one when the row gives it.
    /// </summary>
    /// <param name="fix">The catalogue row.</param>
    /// <remarks>
    /// An edit script runs on a writable <see cref="CompoundFile" /> and is committed; the committed file must render
    /// the expected listing before it is copied.
    /// </remarks>
    [TestMethod]
    [TestCategory("Regression")]
    [DynamicData(
        nameof(RoundTripRows),
        DynamicDataDisplayName = nameof(KatDisplayName.GetDisplayName),
        DynamicDataDisplayNameDeclaringType = typeof(KatDisplayName))]
    public void RoundTrip_WhenFixScenarioIsWrittenBack_ShouldReadBackTheSameListing(ReleaseNoteFix fix)
    {
        ArgumentNullException.ThrowIfNull(fix);

        RowOptions options = RowOptions.Parse(fix.Options);
        string expected = CorpusEscapes.DecodeText(fix.Expected);
        bool edited = !fix.Input.StartsWith(BytesPrefix, StringComparison.Ordinal) && HasEdits(CorpusEscapes.DecodeText(fix.Input));
        byte[] first = edited ? RunEdits(CorpusEscapes.DecodeText(fix.Input)) : ReadContainer(fix.Input);

        string firstListing = OpenAndRender(first, options);
        if (edited)
            Assert.AreEqual(expected, firstListing, $"{fix}: the committed file");

        byte[] copy;
        using (CompoundFile file = CompoundFile.Open(new MemoryStream(first, writable: false), options.ToFileOptions()))
            copy = CompoundStorageBuilder.FromFile(file).ToArray();

        string secondListing = OpenAndRender(copy, options);

        Assert.AreEqual(firstListing, secondListing, $"{fix}: the copy");
        if (expected.Length != 0)
            Assert.AreEqual(expected, secondListing, fix.ToString());
    }
}
