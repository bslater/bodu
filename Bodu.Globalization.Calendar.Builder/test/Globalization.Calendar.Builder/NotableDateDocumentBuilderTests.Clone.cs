// ---------------------------------------------------------------------------------------------------------------
// <copyright file="NotableDateDocumentBuilderTests.Clone.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Globalization.Calendar;

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

        Assert.AreEqual(4, original.Build().NotableDates.Count);
        Assert.AreEqual(5, clone.Build().NotableDates.Count);
    }
}
