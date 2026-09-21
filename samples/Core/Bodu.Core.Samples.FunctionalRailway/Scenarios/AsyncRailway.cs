// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncRailway.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using Bodu.Functional;

namespace Bodu.Core.Samples.FunctionalRailway.Scenarios;

/// <summary>
/// Demonstrates the Task-based async companions (<c>MapAsync</c>/<c>BindAsync</c>/<c>MatchAsync</c>):
/// the same railway composition, but each step is awaitable, so a pipeline of asynchronous
/// operations reads as a single fluent chain. The awaited tasks here complete synchronously so the
/// output stays deterministic.
/// </summary>
public static class AsyncRailway
{
    /// <summary>
    /// Composes an asynchronous fetch → parse → scale pipeline over an already-completed task.
    /// </summary>
    /// <returns>A task that completes when the scenario has printed its output.</returns>
    public static async Task RunAsync()
    {
        SampleConsole.Scenario(
            "Result<T> async - awaitable railway",
            what: "Runs a good and a bad input through the same pipeline as the synchronous railway, except every " +
                  "step is awaitable and composed with MapAsync and BindAsync.",
            why: "Real pipelines call out - a database, an HTTP service - so the railway is only useful if it " +
                 "survives async. Without the async companions each await forces the chain to be unwound into " +
                 "statements with an explicit check between them, which is the shape the railway existed to " +
                 "remove. With them the composition is unchanged and short-circuiting still holds: a failed step " +
                 "means the next one is never awaited, so no wasted call is made.",
            expect: "The same two outcomes as the synchronous version - 21 doubles to 42, and 'nope' reports the " +
                    "parse failure. The awaited tasks complete synchronously here so the transcript stays " +
                    "reproducible; nothing about the composition depends on that.");

        // Each row starts from a Task<Result<string>> and threads through async Map/Bind steps.
        var good = await Pipeline("21");
        var bad = await Pipeline("nope");

        Console.WriteLine($"  \u002721\u0027  : {good.Match(v => $"ok {v}", e => $"error: {e.Message}")}  (expected ok 42 - every awaited step ran in order)");
        Console.WriteLine($"  \u0027nope\u0027: {bad.Match(v => $"ok {v}", e => $"error: {e.Message}")}  (the parse failed, so the doubling step was never awaited - short-circuiting saves the call, not just the result)");

        Console.WriteLine();
    }

    /// <summary>
    /// Fetches the raw value asynchronously, then binds a parse step and maps a scaling step.
    /// </summary>
    /// <param name="key">The key to fetch.</param>
    /// <returns>A task yielding the scaled value or the first error.</returns>
    private static Task<Result<int>> Pipeline(string key) =>
        FetchAsync(key)
            // BindAsync awaits the prior task and lifts the synchronous fallible step into the chain, so no
            // intermediate 'await' is needed between steps.
            .BindAsync(Parse)
            // MapAsync transforms the success value; a failure from any earlier step short-circuits past it.
            .MapAsync(n => n * 2);

    /// <summary>
    /// Simulates an asynchronous fetch that always succeeds (returns the key verbatim).
    /// </summary>
    /// <param name="key">The key to fetch.</param>
    /// <returns>A completed task carrying the fetched value.</returns>
    private static Task<Result<string>> FetchAsync(string key) =>
        Task.FromResult(Result.Success(key));

    /// <summary>
    /// Parses a fetched value into an integer result.
    /// </summary>
    /// <param name="text">The fetched text.</param>
    /// <returns>The parsed value or a parse error.</returns>
    private static Result<int> Parse(string text) =>
        int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
            ? Result.Success(value)
            : Result.Failure<int>(ResultError.FromMessage($"'{text}' is not an integer"));
}
