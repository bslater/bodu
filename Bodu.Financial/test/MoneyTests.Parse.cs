// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MoneyTests.Parse.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial;

public partial class MoneyTests
{
    /// <summary>
    /// Verifies that a bare decimal parses as the correct amount in <typeparamref name="TCurrency" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenBareDecimal_ShouldReturnAmount()
    {
        Money<USD> result = Money<USD>.Parse("19.99", CultureInfo.InvariantCulture);

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }

    /// <summary>
    /// Verifies that a prefixed ISO code matching the currency parses successfully.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIsoCodePrefix_ShouldReturnAmount()
    {
        Money<USD> result = Money<USD>.Parse("USD 19.99", CultureInfo.InvariantCulture);

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }

    /// <summary>
    /// Verifies that a suffixed ISO code matching the currency parses successfully.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIsoCodeSuffix_ShouldReturnAmount()
    {
        Money<USD> result = Money<USD>.Parse("19.99 USD", CultureInfo.InvariantCulture);

        Assert.AreEqual(new Money<USD>(19.99m), result);
    }

    /// <summary>
    /// Verifies that a mismatched ISO code throws <see cref="FormatException" />.
    /// </summary>
    [TestMethod]
    public void Parse_WhenIsoCodeMismatched_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Money<USD>.Parse("JPY 19.99", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that a currency symbol like <c>$</c> is rejected because it is ambiguous.
    /// </summary>
    [TestMethod]
    public void Parse_WhenSymbol_ShouldThrowFormatException()
    {
        Assert.ThrowsExactly<FormatException>(() =>
        {
            _ = Money<USD>.Parse("$19.99", CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.TryParse(string?, IFormatProvider?, out Money{TCurrency})" />
    /// returns <see langword="false" /> for <see langword="null" /> input.
    /// </summary>
    [TestMethod]
    public void TryParse_WhenInputNull_ShouldReturnFalse()
    {
        bool ok = Money<USD>.TryParse((string?)null, CultureInfo.InvariantCulture, out Money<USD> result);

        Assert.IsFalse(ok);
        Assert.AreEqual(default(Money<USD>), result);
    }

    /// <summary>
    /// Verifies that <see cref="Money{TCurrency}.Parse(string, IFormatProvider?)" /> throws
    /// <see cref="ArgumentNullException" /> when supplied a <see langword="null" /> string.
    /// </summary>
    [TestMethod]
    public void Parse_WhenInputNull_ShouldThrowArgumentNullException()
    {
        Assert.ThrowsExactly<ArgumentNullException>(() =>
        {
            _ = Money<USD>.Parse((string)null!, CultureInfo.InvariantCulture);
        });
    }

    /// <summary>
    /// Verifies that parsing round-trips formatting.
    /// </summary>
    [TestMethod]
    [DataRow(19.99)]
    [DataRow(0.0)]
    [DataRow(-1234.56)]
    [DataRow(0.01)]
    public void Parse_WhenRoundTrippingToString_ShouldRecoverOriginal(double amount)
    {
        Money<USD> original = new Money<USD>((decimal)amount);

        string formatted = original.ToString(null, CultureInfo.InvariantCulture);
        Money<USD> recovered = Money<USD>.Parse(formatted, CultureInfo.InvariantCulture);

        Assert.AreEqual(original, recovered);
    }
}
