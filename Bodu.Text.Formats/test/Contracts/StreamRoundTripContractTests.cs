// ---------------------------------------------------------------------------------------------------------------
// <copyright file="StreamRoundTripContractTests.cs" company="Bodu Pty. Ltd.">
//     Copyright (c) Bodu Pty. Ltd.. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Test.Kat;

namespace Bodu.Text.Formats.Contracts;

/// <summary>
/// Reusable behavioural contract test base for the streaming surface of a format with a write/read pair
/// (Bencode, Delimited, DotEnv, INI, etc.). Concrete subclasses override the synchronous and asynchronous
/// adapter methods plus <see cref="ValidCases" /> to plug their format into the inherited tests.
/// </summary>
/// <typeparam name="T">The in-memory document type that round-trips through the stream.</typeparam>
public abstract class StreamRoundTripContractTests<T>
{
    /// <summary>
    /// Writes <paramref name="value" /> synchronously to <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">The destination stream.</param>
    /// <param name="value">The value to serialise.</param>
    protected abstract void Write(Stream destination, T value);

    /// <summary>
    /// Reads a value synchronously from <paramref name="source" />.
    /// </summary>
    /// <param name="source">The source stream.</param>
    /// <returns>The deserialised value.</returns>
    protected abstract T Read(Stream source);

    /// <summary>
    /// Writes <paramref name="value" /> asynchronously to <paramref name="destination" />.
    /// </summary>
    /// <param name="destination">The destination stream.</param>
    /// <param name="value">The value to serialise.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes when the value has been written.</returns>
    protected abstract Task WriteAsync(Stream destination, T value, CancellationToken cancellationToken);

    /// <summary>
    /// Reads a value asynchronously from <paramref name="source" />.
    /// </summary>
    /// <param name="source">The source stream.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>A task that completes with the deserialised value.</returns>
    protected abstract Task<T> ReadAsync(Stream source, CancellationToken cancellationToken);

    /// <summary>
    /// Returns the values that the contract tests round-trip through the stream.
    /// </summary>
    protected abstract IReadOnlyList<BinaryKat<T, T>> ValidCases { get; }

    /// <summary>
    /// Returns an equality comparer used by the contract tests to compare deserialised values against the
    /// originals. Default implementation uses the type's default equality semantics.
    /// </summary>
    protected virtual IEqualityComparer<T> ValueComparer => EqualityComparer<T>.Default;

    /// <summary>
    /// Verifies that every value round-trips through the synchronous write/read pair without loss.
    /// </summary>
    [TestMethod]
    public void RoundTrip_Sync_WhenValid_ShouldYieldOriginalValue()
    {
        foreach (BinaryKat<T, T> kat in ValidCases)
        {
            using MemoryStream buffer = new();
            Write(buffer, kat.Input);
            buffer.Position = 0;
            T actual = Read(buffer);

            Assert.IsTrue(ValueComparer.Equals(kat.Expected, actual), $"KAT '{kat.Name}': sync round-trip lost value identity.");
        }
    }

    /// <summary>
    /// Verifies that every value round-trips through the asynchronous write/read pair without loss.
    /// </summary>
    [TestMethod]
    public async Task RoundTrip_Async_WhenValid_ShouldYieldOriginalValue()
    {
        foreach (BinaryKat<T, T> kat in ValidCases)
        {
            using MemoryStream buffer = new();
            await WriteAsync(buffer, kat.Input, CancellationToken.None);
            buffer.Position = 0;
            T actual = await ReadAsync(buffer, CancellationToken.None);

            Assert.IsTrue(ValueComparer.Equals(kat.Expected, actual), $"KAT '{kat.Name}': async round-trip lost value identity.");
        }
    }
}
