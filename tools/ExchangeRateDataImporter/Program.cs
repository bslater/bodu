// ---------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text.Json;
using Bodu.Financial;
using Bodu.Financial.Currencies;
using Bodu.Financial.ExchangeRates.Caching;

namespace Bodu.Tools.ExchangeRateDataImporter;

/// <summary>
/// Reads provider exchange-rate JSON dumps (one file per year-month-provider) and writes them into the Bodu TOML
/// file-cache layout consumed by the exchange-rate provider infrastructure.
/// </summary>
/// <remarks>
/// Each input file is loaded, its rates are built into <see cref="CachedExchangeRate" /> rows for the file's currency
/// pair, and the rows are saved through the real <see cref="TomlFileExchangeRateCache" />. Reusing the production writer
/// guarantees the emitted <c>&lt;out&gt;/&lt;Provider&gt;/&lt;From&gt;&lt;To&gt;.toml</c> files cannot drift from what
/// the cache reads back, and files sharing a provider and pair merge across runs.
/// </remarks>
internal static class Program
{
    private const string Usage =
        "Usage: dotnet run --project tools/ExchangeRateDataImporter -- --out <dir> [--as-of <iso8601>] <file1.json> [file2.json ...]";

    /// <summary>The shared deserialization options; cached so a new instance is not allocated per file.</summary>
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        PropertyNameCaseInsensitive = true,
    };

    private static int Main(string[] args)
    {
        if (!TryParseArgs(args, out string outputDirectory, out DateTimeOffset stamp, out List<string> inputFiles))
            return 1;

        if (inputFiles.Count == 0)
        {
            Console.Error.WriteLine("No input files supplied.");
            Console.Error.WriteLine(Usage);
            return 1;
        }

        // One cache per provider, reused across that provider's input files so they merge into a single pair file.
        Dictionary<string, TomlFileExchangeRateCache> caches = new(StringComparer.Ordinal);

        int imported = 0;
        foreach (string inputFile in inputFiles)
        {
            if (!File.Exists(inputFile))
            {
                Console.Error.WriteLine($"Input file not found: {inputFile}");
                return 1;
            }

            try
            {
                // 1. Load the JSON dump.
                RateDump dump = ReadDump(inputFile);

                // 2. Build the rates in Bodu.
                ExchangeRatePair pair = BuildPair(dump, inputFile);
                List<CachedExchangeRate> rows = BuildRows(dump, stamp, inputFile);
                if (rows.Count == 0)
                {
                    Console.WriteLine($"Skipped {Path.GetFileName(inputFile)}: no rates.");
                    continue;
                }

                // 3. Save to file through the production cache writer.
                if (!caches.TryGetValue(dump.Provider, out TomlFileExchangeRateCache? cache))
                {
                    cache = new TomlFileExchangeRateCache(new FileExchangeRateCacheOptions
                    {
                        Provider = dump.Provider,
                        CacheDirectory = outputDirectory,
                    });
                    caches[dump.Provider] = cache;
                }

                DateOnly start = rows.Min(static r => r.Date);
                DateOnly end = rows.Max(static r => r.Date);

                // A maximal duration with asOf == CachedAtUtc keeps every imported row; nothing is pruned as stale.
                ExchangeRateCacheWriteStatus status = cache.StoreFetchedRange(pair, rows, start, end, TimeSpan.MaxValue, stamp);
                if (status != ExchangeRateCacheWriteStatus.Stored)
                {
                    Console.Error.WriteLine($"Failed to write {inputFile}: {status}.");
                    return 1;
                }

                Console.WriteLine(
                    $"Imported {Path.GetFileName(inputFile)} [{dump.Provider} {pair.From}/{pair.To}, {rows.Count} rows, {start:yyyy-MM-dd}..{end:yyyy-MM-dd}] -> {cache.ResolveFilePath(pair)}");
                imported++;
            }
            catch (Exception ex) when (ex is InvalidDataException or JsonException)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        Console.WriteLine($"Done. Imported {imported} file(s) across {caches.Count} provider(s) into {outputDirectory}.");
        return 0;
    }

    /// <summary>
    /// Parses the command-line arguments into the output directory, import stamp, and input file list.
    /// </summary>
    /// <param name="args">The raw command-line arguments.</param>
    /// <param name="outputDirectory">When this method returns <see langword="true" />, the resolved output directory.</param>
    /// <param name="stamp">When this method returns <see langword="true" />, the import stamp; <c>--as-of</c> or now.</param>
    /// <param name="inputFiles">When this method returns <see langword="true" />, the resolved input file paths.</param>
    /// <returns><see langword="true" /> when parsing succeeded; otherwise <see langword="false" />.</returns>
    private static bool TryParseArgs(
        string[] args,
        out string outputDirectory,
        out DateTimeOffset stamp,
        out List<string> inputFiles)
    {
        outputDirectory = string.Empty;
        stamp = DateTimeOffset.UtcNow;
        inputFiles = new List<string>();

        string? output = null;
        List<string> inputs = new();

        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out":
                case "-o":
                    if (++i >= args.Length)
                        return Fail("Missing value for --out.");
                    output = args[i];
                    break;

                case "--as-of":
                    if (++i >= args.Length)
                        return Fail("Missing value for --as-of.");
                    if (!DateTimeOffset.TryParse(args[i], CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out stamp))
                        return Fail($"Invalid --as-of value '{args[i]}'; expected an ISO 8601 instant.");
                    break;

                default:
                    inputs.Add(args[i]);
                    break;
            }
        }

        if (string.IsNullOrWhiteSpace(output))
            return Fail("The --out <dir> option is required.");

        outputDirectory = output!;

        // A directory argument is expanded to every *.json file beneath it, recursing into subfolders.
        foreach (string input in inputs)
        {
            if (Directory.Exists(input))
                inputFiles.AddRange(Directory.EnumerateFiles(input, "*.json", SearchOption.AllDirectories));
            else
                inputFiles.Add(input);
        }

        return true;
    }

    /// <summary>
    /// Writes an error message and the usage line to standard error and reports a parse failure.
    /// </summary>
    /// <param name="message">The error message to display.</param>
    /// <returns>Always <see langword="false" />, so callers can <c>return Fail(...)</c>.</returns>
    private static bool Fail(string message)
    {
        Console.Error.WriteLine(message);
        Console.Error.WriteLine(Usage);
        return false;
    }

    /// <summary>
    /// Reads and deserializes a provider rate dump from disk.
    /// </summary>
    /// <param name="path">The path of the JSON file to read.</param>
    /// <returns>The deserialized <see cref="RateDump" />.</returns>
    /// <exception cref="InvalidDataException">
    /// Thrown when the file cannot be deserialized or does not specify a provider.
    /// </exception>
    private static RateDump ReadDump(string path)
    {
        string json = File.ReadAllText(path);
        RateDump? dump = JsonSerializer.Deserialize<RateDump>(json, s_jsonOptions);
        if (dump is null)
            throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, ExchangeRateDataImporterResourceStrings.IO_Invalid_Deserialize, path));

        if (string.IsNullOrWhiteSpace(dump.Provider))
            throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, ExchangeRateDataImporterResourceStrings.IO_Invalid_ProviderMissing, path));

        return dump;
    }

    /// <summary>
    /// Builds the directional currency pair from a dump's base and target currency codes.
    /// </summary>
    /// <param name="dump">The dump whose currency codes are mapped.</param>
    /// <param name="path">The source file path, used for diagnostics.</param>
    /// <returns>The resolved <see cref="ExchangeRatePair" />.</returns>
    /// <exception cref="InvalidDataException">Thrown when either currency code is unknown.</exception>
    private static ExchangeRatePair BuildPair(RateDump dump, string path) =>
        new(ParseCurrency(dump.BaseCurrency, path), ParseCurrency(dump.TargetCurrency, path));

    /// <summary>
    /// Maps an ISO 4217 alphabetic code (case-insensitive) to a <see cref="CurrencyCode" />.
    /// </summary>
    /// <param name="code">The three-letter currency code.</param>
    /// <param name="path">The source file path, used for diagnostics.</param>
    /// <returns>The resolved <see cref="CurrencyCode" />.</returns>
    /// <exception cref="InvalidDataException">Thrown when the code is empty, unknown, or the <c>None</c> sentinel.</exception>
    private static CurrencyCode ParseCurrency(string code, string path)
    {
        if (!Enum.TryParse(code, ignoreCase: true, out CurrencyCode parsed) || parsed == CurrencyCode.None || !Enum.IsDefined(parsed))
            throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, ExchangeRateDataImporterResourceStrings.IO_Invalid_Currency, code, path));

        return parsed;
    }

    /// <summary>
    /// Builds the cached rate rows from a dump's date-keyed rate map, stamping each with the import instant.
    /// </summary>
    /// <param name="dump">The dump whose rates are converted.</param>
    /// <param name="stamp">The import instant recorded as each row's caching instant.</param>
    /// <param name="path">The source file path, used for diagnostics.</param>
    /// <returns>The cached rate rows; empty when the dump carries no rates.</returns>
    /// <exception cref="InvalidDataException">Thrown when a rate key is not a <c>yyyy-MM-dd</c> date.</exception>
    private static List<CachedExchangeRate> BuildRows(RateDump dump, DateTimeOffset stamp, string path)
    {
        List<CachedExchangeRate> rows = new(dump.Rates.Count);
        foreach (KeyValuePair<string, decimal> entry in dump.Rates)
        {
            if (!DateOnly.TryParseExact(entry.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly date))
                throw new InvalidDataException(string.Format(CultureInfo.CurrentCulture, ExchangeRateDataImporterResourceStrings.IO_Invalid_Date, entry.Key, path));

            // The source supplies no upstream fetch instant, so ObservedAtUtc is left null.
            rows.Add(new CachedExchangeRate(date, entry.Value, stamp, ObservedAtUtc: null));
        }

        return rows;
    }

    /// <summary>
    /// The serialization shape of a provider exchange-rate dump: a single currency pair and its date-keyed rates.
    /// </summary>
    private sealed record RateDump
    {
        /// <summary>Gets the provider identifier the rates were sourced from.</summary>
        /// <value>The provider name, used as the cache subdirectory.</value>
        public string Provider { get; init; } = string.Empty;

        /// <summary>Gets the base (source) currency ISO code.</summary>
        /// <value>The base currency code.</value>
        public string BaseCurrency { get; init; } = string.Empty;

        /// <summary>Gets the target (quote) currency ISO code.</summary>
        /// <value>The target currency code.</value>
        public string TargetCurrency { get; init; } = string.Empty;

        /// <summary>Gets the rates keyed by <c>yyyy-MM-dd</c> observation date.</summary>
        /// <value>The date-keyed rate map.</value>
        public Dictionary<string, decimal> Rates { get; init; } = new();
    }
}
