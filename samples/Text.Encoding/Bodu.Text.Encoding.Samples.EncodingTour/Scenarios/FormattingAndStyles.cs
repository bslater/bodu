// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FormattingAndStyles.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Text.Encoding;

namespace Bodu.Samples.Text.Encoding.EncodingTour.Scenarios;

/// <summary>
/// Demonstrates the two option enums that bracket every codec: <see cref="BaseFormattingOptions" />
/// shapes the text you produce (case, spacing, prefixes, padding), and
/// <see cref="BaseFormatStyles" /> declares what you tolerate when parsing text produced by
/// someone else (prefixes, whitespace, missing padding).
/// </summary>
public static class FormattingAndStyles
{
    /// <summary>
    /// Produces decorated output and parses decorated input.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Formatting on write, tolerance on read",
            what: "Encodes one payload with each formatting option - case, prefix, spacing, omitted padding - then "
                + "parses a decorated string that strict parsing rejects, and an unpadded one, by naming exactly "
                + "which deviations to accept.",
            why: "These are two different questions and the API keeps them apart. What you emit should be as "
                + "close to canonical as the consumer allows, because every decoration is something a downstream "
                + "parser has to be taught about. What you accept is a separate decision, made once per input "
                + "source: a hex dump pasted from a debugger carries 0x and spaces, a JWT segment has its padding "
                + "stripped, and neither is your bug to reject on principle. The failure mode this design avoids "
                + "is a parser that is quietly permissive about everything, where a typo that should have been an "
                + "error becomes wrong bytes instead.",
            expect: "Each formatting option changes only the presentation - decode any of these rows and the same "
                + "six bytes come back. Strict parsing rejects the decorated input rather than guessing, and the "
                + "same input succeeds once the two tolerated deviations are named. Tolerance is opt-in per call, "
                + "so widening it for one untrusted source does not widen it everywhere.");

        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x23 };

        // Formatting options decorate the output.
        Console.WriteLine($"  default        : {Base16.Encode(payload)}"
            + "  (the canonical form - what to emit unless a consumer demands otherwise)");
        Console.WriteLine($"  UpperCase      : {Base16.Encode(payload, BaseFormattingOptions.UpperCase)}");
        Console.WriteLine($"  IncludePrefix  : {Base16.Encode(payload, BaseFormattingOptions.IncludePrefix)}");
        Console.WriteLine($"  InsertSpacing  : {Base16.Encode(payload, BaseFormattingOptions.InsertSpacing)}");
        Console.WriteLine($"  OmitPadding    : {Base64.Encode(payload, options: BaseFormattingOptions.OmitPadding)}"
            + "  (no trailing '=' - the convention JWTs and URL fragments use, since the length implies the padding)");

        // Format styles declare what the parser should tolerate.
        var decorated = "0xDE AD BE EF 01 23";
        var strictFails = !Base16.IsValid(decorated);
        var lenient = Base16.Decode(decorated, BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        Console.WriteLine();
        Console.WriteLine($"  input '{decorated}'");
        Console.WriteLine($"  strict parse rejects   : {strictFails}"
            + "  (expected True - by default the prefix and spaces are errors, not noise to be skipped)");
        Console.WriteLine($"  AllowPrefix|IgnoreWs   : {lenient.Length} bytes recovered -> {lenient.AsSpan().SequenceEqual(payload)}"
            + "  (expected True - naming the two deviations accepts them without accepting anything else)");

        var unpadded = Base64.Encode(payload, options: BaseFormattingOptions.OmitPadding);
        var reparsed = Base64.Decode(unpadded, style: BaseFormatStyles.AllowMissingPadding);
        Console.WriteLine($"  AllowMissingPadding    : '{unpadded}' -> {reparsed.Length} bytes"
            + "  (the read-side counterpart of OmitPadding - the two options are how a codec round-trips through a padding-hostile channel)");

        Console.WriteLine();
    }
}
