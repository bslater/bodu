// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Clone.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Globalization.Calendar.Builder;

public partial class NotableDateDocumentBuilderTests
{
    /// <summary>
    /// Verifies that mutating a cloned document builder does not affect the original.
    /// </summary>
    [TestMethod]
    public void Clone_WhenCloneMutated_ShouldNotAffectOriginal()
    {
        NotableDateDocumentBuilder original = SampleDocument();

        NotableDateDocumentBuilder clone = original.Clone();
        clone.AddNotableDate("independence-day", "Independence Day", NotableDateCategory.PublicHoliday, d => d
            .AddRule("default", r => r.ForTerritory("US").Fixed(7, 4)));

        // The original keeps its four definitions while the mutated clone gains the fifth.
        Assert.AreEqual(
            (4, 5),
            (original.Build().NotableDates.Count, clone.Build().NotableDates.Count));
    }
}
