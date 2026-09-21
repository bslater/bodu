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
        SampleConsole.Scenario(
            "Compute - issuing identifiers with their check digit",
            what: "Computes the check digit for a payload under four schemes and appends it, including the " +
                  "ISBN-10 case that produces 'X' and an EAN-13 computed from a streamed payload.",
            why: "Validation and issuance are the same arithmetic run in opposite directions, so the compute side " +
                  "is what an issuer needs - generating an account number, a barcode or an ISBN that every " +
                  "downstream validator will accept. The ISBN-10 case is the one worth knowing: its check value " +
                  "ranges 0-10, so ten is written as 'X'. Code that assumes a check digit is always a digit " +
                  "produces an identifier that fails everywhere.",
            expect: "Each payload plus its computed digit validates as a whole, which is the round trip. ISBN-10 " +
                    "yields the literal character X for the value ten, and the streamed EAN-13 gives the same " +
                    "answer as a one-shot call.");

        // A card issuer appends the Luhn digit to the account payload.
        var cardPayload = "7992739871";
        var cardCheck = Luhn.Compute(cardPayload);
        Console.WriteLine($"  Luhn    : payload {cardPayload} + check \u0027{cardCheck}\u0027 = {cardPayload}{cardCheck}  (valid: {Luhn.IsValid(cardPayload + cardCheck)} - issuing and validating are the same arithmetic run in opposite directions)");

        // A publisher derives the ISBN-13 check digit for a new title's prefix+group+registrant.
        var isbnPayload = "978030640615";
        var isbnCheck = Isbn13.Compute(isbnPayload);
        Console.WriteLine($"  ISBN-13 : payload {isbnPayload} + check \u0027{isbnCheck}\u0027 = {isbnPayload}{isbnCheck}  (the same digit any downstream validator will recompute and compare)");

        // ISBN-10's alphabet includes 'X' (value 10) - the check digit is not always decimal.
        var isbnXPayload = "097522980";
        Console.WriteLine($"  ISBN-10 : payload {isbnXPayload} + check \u0027{Isbn10.Compute(isbnXPayload)}\u0027  (X is the value TEN - code that assumes a check digit is always a digit issues identifiers that fail everywhere)");

        // The streaming surface: feed the payload in fragments, then read the digit -
        // the same shape as the hashing side's Append/GetCurrentHash.
        var streaming = new Ean13();
        streaming.Append("400638");
        streaming.Append("133393");
        Console.WriteLine($"  EAN-13  : streamed 400638|133393 -> check \u0027{streaming.GetCurrentCheckDigit()}\u0027  ({streaming.AlgorithmName} fed in two chunks gives the same answer as one call - the state is incremental)");

        Console.WriteLine();
    }
}
