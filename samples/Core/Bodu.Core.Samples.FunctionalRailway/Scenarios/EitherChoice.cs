// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EitherChoice.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Functional;

namespace Bodu.Core.Samples.FunctionalRailway.Scenarios;

/// <summary>
/// Demonstrates <see cref="Either{TLeft, TRight}" /> as a typed either/or: a value is exactly one
/// of two shapes, and both sides are first-class. Unlike <c>Result</c> (where one side is
/// specifically an error) neither side of an <c>Either</c> is privileged.
/// </summary>
public static class EitherChoice
{
    /// <summary>
    /// Classifies inputs into a left or right branch and maps each side independently.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Either<TLeft, TRight>: a typed choice ---");

        // A payment is either a card token (left) or a raw bank account (right); both are valid.
        foreach (var input in new[] { "card:4111", "iban:DE89", "card:5500" })
        {
            Either<string, string> method = Classify(input);

            // MapLeft/MapRight transform one branch and leave the other untouched.
            Either<string, string> masked = method
                .MapLeft(token => $"card ****{token[^2..]}")
                .MapRight(iban => $"bank {iban}");

            // Match collapses both branches into one display string.
            var rendered = masked.Match(onLeft: card => card, onRight: bank => bank);
            Console.WriteLine($"{input,-10} -> isLeft={method.IsLeft,-5} {rendered}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Routes an input string to the left branch (card) or the right branch (bank).
    /// </summary>
    /// <param name="input">The raw payment descriptor.</param>
    /// <returns>A left either for a card token, otherwise a right either for a bank identifier.</returns>
    private static Either<string, string> Classify(string input) =>
        input.StartsWith("card:", StringComparison.Ordinal)
            ? Either<string, string>.Left(input["card:".Length..])
            : Either<string, string>.Right(input["iban:".Length..]);
}
