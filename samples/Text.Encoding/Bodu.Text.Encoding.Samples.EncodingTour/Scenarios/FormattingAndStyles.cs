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
        Console.WriteLine("--- BaseFormattingOptions (write) and BaseFormatStyles (read) ---");

        var payload = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x23 };

        // Formatting options decorate the output.
        Console.WriteLine($"default        : {Base16.Encode(payload)}");
        Console.WriteLine($"UpperCase      : {Base16.Encode(payload, BaseFormattingOptions.UpperCase)}");
        Console.WriteLine($"IncludePrefix  : {Base16.Encode(payload, BaseFormattingOptions.IncludePrefix)}");
        Console.WriteLine($"InsertSpacing  : {Base16.Encode(payload, BaseFormattingOptions.InsertSpacing)}");
        Console.WriteLine($"OmitPadding    : {Base64.Encode(payload, options: BaseFormattingOptions.OmitPadding)}");

        // Format styles declare what the parser should tolerate.
        var decorated = "0xDE AD BE EF 01 23";
        var strictFails = !Base16.IsValid(decorated);
        var lenient = Base16.Decode(decorated, BaseFormatStyles.AllowPrefix | BaseFormatStyles.IgnoreWhitespace);

        Console.WriteLine();
        Console.WriteLine($"input '{decorated}'");
        Console.WriteLine($"strict parse rejects   : {strictFails}");
        Console.WriteLine($"AllowPrefix|IgnoreWs   : {lenient.Length} bytes recovered -> {lenient.AsSpan().SequenceEqual(payload)}");

        var unpadded = Base64.Encode(payload, options: BaseFormattingOptions.OmitPadding);
        var reparsed = Base64.Decode(unpadded, style: BaseFormatStyles.AllowMissingPadding);
        Console.WriteLine($"AllowMissingPadding    : '{unpadded}' -> {reparsed.Length} bytes");

        Console.WriteLine();
    }
}
