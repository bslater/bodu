// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CalendarValueSetContractTests{T}.IParsable.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Test.Kat;

namespace Bodu.Contracts;

public abstract partial class CalendarValueSetContractTests<TSet>
{
    /// <summary>
    /// Verifies that <see cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" /> reads the canonical rows as
    /// <c>Parse</c> does, whatever the provider.
    /// </summary>
    [TestMethod]
    public void IParsableParse_WhenProviderSupplied_ShouldIgnoreIt()
    {
        IFormatProvider?[] providers = [null, CultureInfo.InvariantCulture, CultureInfo.GetCultureInfo("ar-SA"), CultureInfo.GetCultureInfo("fr-FR")];

        foreach (ValidKat<string, TSet> kat in CanonicalCases())
        {
            foreach (IFormatProvider? provider in providers)
                Assert.AreEqual(kat.Expected, ParseString(kat.Input, provider), $"{kat.Name}, provider {provider}");
        }
    }

    /// <summary>
    /// Verifies that <see cref="IParsable{TSelf}.Parse(string, IFormatProvider?)" /> throws
    /// <see cref="FormatException" /> for malformed text.
    /// </summary>
    [TestMethod]
    public void IParsableParse_WhenTextIsMalformed_ShouldThrowFormatException()
    {
        foreach (InvalidKat<string> kat in MalformedCases())
        {
            Assert.ThrowsExactly<FormatException>(() =>
            {
                _ = ParseString(kat.Input, CultureInfo.InvariantCulture);
            }, kat.Name);
        }
    }

    /// <summary>
    /// Verifies that <see cref="IParsable{TSelf}.TryParse(string?, IFormatProvider?, out TSelf)" /> answers as
    /// <c>TryParse</c> does, whatever the provider.
    /// </summary>
    [TestMethod]
    public void IParsableTryParse_WhenCalled_ShouldAnswerAsTryParse()
    {
        foreach (ValidKat<string, TSet> kat in CanonicalCases())
        {
            Assert.IsTrue(TryParseString(kat.Input, CultureInfo.GetCultureInfo("ar-SA"), out TSet result), kat.Name);
            Assert.AreEqual(kat.Expected, result, kat.Name);
        }

        foreach (InvalidKat<string> kat in MalformedCases())
            Assert.IsFalse(TryParseString(kat.Input, CultureInfo.GetCultureInfo("ar-SA"), out _), kat.Name);

        Assert.IsFalse(TryParseString(null, CultureInfo.InvariantCulture, out _));
    }
}
