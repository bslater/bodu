// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ResultRailway.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Functional;

namespace Bodu.Core.Samples.FunctionalRailway.Scenarios;

/// <summary>
/// Demonstrates the railway pattern with <see cref="Result{T}" />: a validate → parse → transform
/// pipeline composed with <c>Bind</c>/<c>Map</c>/<c>Tap</c>. The first failing step short-circuits
/// the rest, carrying a <see cref="ResultError" /> to the end instead of throwing.
/// </summary>
/// <remarks>
/// The point is that failure is a return value on the same path as success, so the pipeline reads top to bottom
/// with no try/catch interrupting it. Each step is written as if the previous one succeeded — because if it did
/// not, the step never runs — and the original error arrives intact at the end rather than being wrapped, logged
/// and rethrown at every level.
/// </remarks>
public static class ResultRailway
{
    /// <summary>
    /// Runs one valid input and one invalid input through the same pipeline.
    /// </summary>
    public static void Run()
    {
        SampleConsole.Scenario(
            "Result<T> - validate -> parse -> transform railway",
            what: "Runs four inputs through one validate-then-parse-then-double pipeline: a good value and three " +
                  "that fail at three different steps.",
            why: "Exceptions for expected failures cost you the control flow: the happy path gets interleaved with " +
                 "handlers, and an error is easy to catch too broadly or too late. A railway keeps failure on the " +
                 "same return path, so the first failing step short-circuits the rest and the error travels to the " +
                 "end untouched. The steps stay individually simple because each is written assuming the previous " +
                 "one succeeded - which is guaranteed, since otherwise it does not run at all.",
            expect: "Only '42' reaches the end, doubled to 84. The other three each stop at a different step and " +
                    "report why, in the vocabulary of the step that failed - blank input, unparseable text, and a " +
                    "negative value - rather than one generic message.");

        Console.WriteLine($"  \u002742\u0027   : {Format(Process("42"))}  (expected ok 84 - the only input that clears all three steps)");
        Console.WriteLine($"  \u0027 -3 \u0027 : {Format(Process(" -3 "))}  (parsed fine, then failed the range rule - so the error names the value, not the format)");
        Console.WriteLine($"  \u0027oops\u0027 : {Format(Process("oops"))}  (failed one step earlier, at the parse; the doubling step never ran)");
        Console.WriteLine($"  \u0027\u0027     : {Format(Process(string.Empty))}  (failed at the first step, so neither the parse nor the transform was reached)");

        Console.WriteLine();
    }

    /// <summary>
    /// Composes the three steps; any failure diverts onto the error track for the remainder.
    /// </summary>
    /// <param name="raw">The raw user input.</param>
    /// <returns>The doubled, validated integer or the first error encountered.</returns>
    private static Result<int> Process(string raw) =>
        Validate(raw)
            // Bind chains a step that can itself fail; once any step fails, later steps are skipped entirely.
            .Bind(Parse)
            .Tap(n => { /* side effect on success only - e.g. logging */ })
            // Map applies an infallible transform to the success value; an error flows past it unchanged.
            .Map(n => n * 2);

    /// <summary>
    /// Rejects blank input before any parsing is attempted.
    /// </summary>
    /// <param name="raw">The raw input.</param>
    /// <returns>The trimmed input on success, or a validation error.</returns>
    private static Result<string> Validate(string raw) =>
        string.IsNullOrWhiteSpace(raw)
            ? Result.Failure<string>(ResultError.FromMessage("input was blank"))
            : Result.Success(raw.Trim());

    /// <summary>
    /// Parses the (already non-blank) input as a positive integer.
    /// </summary>
    /// <param name="text">The trimmed input.</param>
    /// <returns>The parsed value on success, or a parse/range error.</returns>
    private static Result<int> Parse(string text)
    {
        if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
            return Result.Failure<int>(ResultError.FromMessage($"'{text}' is not an integer"));

        return value < 0
            ? Result.Failure<int>(ResultError.FromMessage($"{value} is negative"))
            : Result.Success(value);
    }

    /// <summary>
    /// Renders a result as a short human-readable string.
    /// </summary>
    /// <param name="result">The result to format.</param>
    /// <returns>The success value or the failure message.</returns>
    private static string Format(Result<int> result) =>
        result.Match(value => $"ok {value}", error => $"error: {error.Message}");
}
