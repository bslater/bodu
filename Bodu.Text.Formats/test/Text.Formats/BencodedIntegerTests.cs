// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BencodedIntegerTests.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Text.Formats;

/// <summary>
/// Behavioural tests for <see cref="BencodedInteger" />. Partial files split coverage by member or subject.
/// </summary>
[TestClass]
public sealed partial class BencodedIntegerTests
{

    /// <summary>
    /// Verifies that the constructor stores the supplied value and exposes <see cref="BencodedValueKind.Integer" />
    /// via <see cref="BencodedValue.Kind" />.
    /// </summary>
    /// <param name="value">The integer value under test.</param>
    [TestMethod]
    [DataRow(0L)]
    [DataRow(1L)]
    [DataRow(-1L)]
    [DataRow(42L)]
    [DataRow(long.MaxValue)]
    [DataRow(long.MinValue)]
    public void Constructor_WhenValueProvided_ShouldStoreValueAndReportIntegerKind(long value)
    {
        BencodedInteger integer = new(value);

        Assert.AreEqual(value, integer.Value);
        Assert.AreEqual(BencodedValueKind.Integer, integer.Kind);
    }

    /// <summary>
    /// Verifies that <see cref="BencodedInteger.ToString" /> formats the value using invariant culture, so it is
    /// stable regardless of the current thread culture (which may use non-ASCII digit groups or decimal markers).
    /// </summary>
    [TestMethod]
    public void ToString_WhenCurrentCultureIsNonInvariant_ShouldFormatWithInvariantCulture()
    {
        System.Globalization.CultureInfo previous = System.Globalization.CultureInfo.CurrentCulture;

        try
        {
            // de-DE uses '.' as a thousands separator, but BencodedInteger.ToString should ignore it.
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");

            BencodedInteger integer = new(1000);

            Assert.AreEqual("1000", integer.ToString());
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = previous;
        }
    }

}
