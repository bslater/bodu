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
public static class ResultRailway
{
    /// <summary>
    /// Runs one valid input and one invalid input through the same pipeline.
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("--- Result<T>: validate -> parse -> transform railway ---");

        Console.WriteLine($"'42'   : {Format(Process("42"))}");
        Console.WriteLine($"' -3 ' : {Format(Process(" -3 "))}");
        Console.WriteLine($"'oops' : {Format(Process("oops"))}");
        Console.WriteLine($"''     : {Format(Process(string.Empty))}");

        Console.WriteLine();
    }

    /// <summary>
    /// Composes the three steps; any failure diverts onto the error track for the remainder.
    /// </summary>
    /// <param name="raw">The raw user input.</param>
    /// <returns>The doubled, validated integer or the first error encountered.</returns>
    private static Result<int> Process(string raw) =>
        Validate(raw)
            .Bind(Parse)
            .Tap(n => { /* side effect on success only - e.g. logging */ })
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
