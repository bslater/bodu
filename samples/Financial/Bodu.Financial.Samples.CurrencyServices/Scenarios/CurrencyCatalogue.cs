// ---------------------------------------------------------------------------------------------------------------
// <copyright file="CurrencyCatalogue.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Financial.Currencies;

namespace Bodu.Financial.Samples.CurrencyServices.Scenarios;

/// <summary>
/// Demonstrates the currency catalogue surface: the shipped ISO 4217 registry
/// (<see cref="CurrencyRegistry" />), the <see cref="CurrencyInfo" /> record it stores, the
/// <see cref="CurrencyCode" /> enum bridge, and the indexed <see cref="CurrencyLookupService" />
/// (<see cref="ICurrencyLookup" />). The headline fact: minor units are per-currency data, not a
/// universal "2 decimal places" — JPY has 0, BHD has 3.
/// </summary>
public static class CurrencyCatalogue
{
    /// <summary>
    /// Reports the registry size, then looks up well-known currencies and prints their metadata.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "The currency catalogue - registry, metadata, and lookup",
            what: "Reads currencies out of the registry, inspects the metadata each one carries, and resolves "
                + "codes through the lookup service including one that does not exist.",
            why: "Currency is not just a three-letter label - the number of minor units, the cash rounding "
                + "increment and the symbol are all properties of the currency, and code that hardcodes two "
                + "decimals is wrong for the Japanese yen and for the Tunisian dinar in opposite directions. "
                + "Shipping the ISO 4217 catalogue means those facts come from one place rather than from "
                + "whatever each call site assumed. The lookup service exists because currency codes usually "
                + "arrive as data, so resolution has to handle the unknown code as a normal outcome rather than "
                + "as an exception.",
            expect: "Each currency reports its own minor units rather than an assumed two, which is what makes "
                + "the money types correct for zero- and three-decimal currencies. An unrecognised code resolves "
                + "to a miss rather than throwing.");

        // The shipped registry is the whole ISO 4217 catalogue.
        Console.WriteLine($"  Registry: {CurrencyRegistry.All.Count} currencies shipped");

        // The indexed lookup service is what runtime code queries by ISO code, numeric code, symbol,
        // region, or culture. Look up three currencies with different minor-unit conventions.
        ICurrencyLookup lookup = new CurrencyLookupService();

        foreach (var isoCode in new[] { "USD", "JPY", "BHD" })
        {
            if (lookup.TryByIsoCode(isoCode, out CurrencyInfo info))
            {
                var symbol = string.IsNullOrEmpty(info.Symbol) ? "(none)" : info.Symbol;
                Console.WriteLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "  {0} ({1,3}) {2}  symbol {3,-4}  {4} minor unit(s)",
                    info.IsoCode,
                    info.NumericCode,
                    info.EnglishName,
                    symbol,
                    info.MinorUnits));
            }
        }

        // Minor units drive amount scale: 2 decimals for most currencies, but 0 for JPY and 3 for BHD.
        Console.WriteLine("  JPY carries 0 minor units (whole yen); BHD carries 3 (thousandths of a dinar).");

        // The CurrencyCode enum is the strongly-typed bridge into the same registry entry.
        CurrencyInfo usd = CurrencyInfo.FromCurrencyCode(CurrencyCode.USD);
        Console.WriteLine($"  Enum bridge: CurrencyCode.USD -> {usd.IsoCode} #{usd.NumericCode}");

        Console.WriteLine();
    }
}
