// ---------------------------------------------------------------------------------------------------------------
// <copyright file="DayOfWeekSetTests.IParsable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu;

public partial class DayOfWeekSetTests
{
    /// <summary>
    /// Verifies that <see cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" /> reads a mask as
    /// <see cref="DayOfWeekSet.Parse(string)" /> does, whatever the provider.
    /// </summary>
    [TestMethod]
    [DataRow("_MTWTF_")]
    [DataRow("MTWTF__")]
    [DataRow("0111110")]
    [DataRow("S-----S")]
    public void IParsableParse_WhenProviderSupplied_ShouldIgnoreIt(string text)
    {
        IFormatProvider?[] providers = [null, CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("ar-SA"), CultureInfo.GetCultureInfo("fr-FR")];

        foreach (IFormatProvider? provider in providers)
            Assert.AreEqual(DayOfWeekSet.Parse(text), ParseThroughInterface<DayOfWeekSet>(text, provider), $"provider {provider}");
    }

    /// <summary>
    /// Verifies that <see cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" /> throws
    /// <see cref="FormatException" /> for text that is not a mask.
    /// </summary>
    [TestMethod]
    public void IParsableParse_WhenTextIsNotMask_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = ParseThroughInterface<DayOfWeekSet>("MF", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" /> answers as
    /// <see cref="DayOfWeekSet.TryParse(string?, out DayOfWeekSet)" /> does.
    /// </summary>
    [TestMethod]
    public void IParsableTryParse_WhenCalled_ShouldAnswerAsTryParse()
    {
        Assert.IsTrue(TryParseThroughInterface("_MTWTF_", CultureInfo.InvariantCulture, out DayOfWeekSet weekdays));
        Assert.AreEqual(DayOfWeekSet.Weekdays, weekdays);

        Assert.IsFalse(TryParseThroughInterface("MF", CultureInfo.InvariantCulture, out DayOfWeekSet failed));
        Assert.AreEqual(DayOfWeekSet.Empty, failed);

        Assert.IsFalse(TryParseThroughInterface(null, CultureInfo.InvariantCulture, out _));
    }

    /// <summary>
    /// Calls <see cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" /> on a type.
    /// </summary>
    /// <typeparam name="T">The type to parse.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <returns>The parsed value.</returns>
    private static T ParseThroughInterface<T>(string text, IFormatProvider? provider)
        where T : IParsable<T> =>
        T.Parse(text, provider);

    /// <summary>
    /// Calls <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" /> on
    /// <see cref="DayOfWeekSet" />.
    /// </summary>
    /// <param name="text">The text to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">The parsed set, or the default when parsing fails.</param>
    /// <returns>The type's answer.</returns>
    private static bool TryParseThroughInterface(string? text, IFormatProvider? provider, out DayOfWeekSet result) =>
        TryParseThroughInterface<DayOfWeekSet>(text, provider, out result);

    /// <summary>
    /// Calls <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" /> on a type.
    /// </summary>
    /// <typeparam name="T">The type to parse.</typeparam>
    /// <param name="text">The text to parse.</param>
    /// <param name="provider">The format provider.</param>
    /// <param name="result">The parsed value, or the default when parsing fails.</param>
    /// <returns>The type's answer.</returns>
    private static bool TryParseThroughInterface<T>(string? text, IFormatProvider? provider, out T result)
        where T : IParsable<T> =>
        T.TryParse(text, provider, out result!);
}
