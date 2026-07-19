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
        Console.WriteLine("--- Result<T> async: awaitable railway ---");

        // Each row starts from a Task<Result<string>> and threads through async Map/Bind steps.
        var good = await Pipeline("21");
        var bad = await Pipeline("nope");

        Console.WriteLine($"'21'  : {good.Match(v => $"ok {v}", e => $"error: {e.Message}")}");
        Console.WriteLine($"'nope': {bad.Match(v => $"ok {v}", e => $"error: {e.Message}")}");

        Console.WriteLine();
    }

    /// <summary>
    /// Fetches the raw value asynchronously, then binds a parse step and maps a scaling step.
    /// </summary>
    /// <param name="key">The key to fetch.</param>
    /// <returns>A task yielding the scaled value or the first error.</returns>
    private static Task<Result<int>> Pipeline(string key) =>
        FetchAsync(key)
            .BindAsync(Parse)
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
