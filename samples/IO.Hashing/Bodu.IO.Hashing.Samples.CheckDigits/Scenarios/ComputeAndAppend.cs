// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ComputeAndAppend.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Hashing.CheckDigits;

namespace Bodu.IO.Hashing.Samples.CheckDigits.Scenarios;

/// <summary>
/// Demonstrates the generation direction: issuing identifiers means computing the check digit
/// for a payload — one static <c>Compute</c> call — plus the streaming
/// <c>Append</c>/<c>GetCurrentCheckDigit</c> surface for payloads that arrive in fragments.
/// </summary>
public static class ComputeAndAppend
{
    /// <summary>
    /// Generates check digits for new identifiers and shows the streaming equivalent.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Compute: issuing identifiers with their check digit ---");

        // A card issuer appends the Luhn digit to the account payload.
        var cardPayload = "7992739871";
        var cardCheck = Luhn.Compute(cardPayload);
        Console.WriteLine($"Luhn    : payload {cardPayload} + check '{cardCheck}' = {cardPayload}{cardCheck} (valid: {Luhn.IsValid(cardPayload + cardCheck)})");

        // A publisher derives the ISBN-13 check digit for a new title's prefix+group+registrant.
        var isbnPayload = "978030640615";
        var isbnCheck = Isbn13.Compute(isbnPayload);
        Console.WriteLine($"ISBN-13 : payload {isbnPayload} + check '{isbnCheck}' = {isbnPayload}{isbnCheck}");

        // ISBN-10's alphabet includes 'X' (value 10) - the check digit is not always decimal.
        var isbnXPayload = "097522980";
        Console.WriteLine($"ISBN-10 : payload {isbnXPayload} + check '{Isbn10.Compute(isbnXPayload)}' ('X' = value 10)");

        // The streaming surface: feed the payload in fragments, then read the digit -
        // the same shape as the hashing side's Append/GetCurrentHash.
        var streaming = new Ean13();
        streaming.Append("400638");
        streaming.Append("133393");
        Console.WriteLine($"EAN-13  : streamed 400638|133393 -> check '{streaming.GetCurrentCheckDigit()}' ({streaming.AlgorithmName})");

        Console.WriteLine();
    }
}
