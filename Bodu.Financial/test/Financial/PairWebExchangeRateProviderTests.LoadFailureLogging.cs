// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebExchangeRateProviderTests.LoadFailureLogging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial;

public partial class PairWebExchangeRateProviderTests
{
    /// <summary>The event id of the <c>PairLoadFailed</c> diagnostic emitted when a pair download fails.</summary>
    private const int PairLoadFailedEventId = 4403;

    /// <summary>The event id of the <c>PairLoadUnexpectedError</c> diagnostic emitted when a pair download throws an unexpected exception type.</summary>
    private const int PairLoadUnexpectedErrorEventId = 4406;

    /// <summary>
    /// Creates a provider whose source throws the supplied exception, capturing its log output.
    /// </summary>
    /// <param name="exception">The exception the source throws from every fetch.</param>
    /// <returns>The provider and its capturing logger.</returns>
    private static (TestPairWebExchangeRateProvider Provider, CapturingLogger Logger) CreateThrowing(Exception exception)
    {
        CapturingLogger logger = new();
        TestPairWebExchangeRateProvider provider = new(new ThrowingPairSource(exception), new TestWebExchangeRateProviderOptions(), logger);

        return (provider, logger);
    }

    /// <summary>
    /// Verifies that an unexpected exception type thrown by the source — indicating a provider bug rather than a
    /// transport or data failure — is logged as a distinct unexpected error at <see cref="LogLevel.Error" /> and
    /// rethrown, without being relabelled as an ordinary pair-load failure.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenSourceThrowsUnexpectedException_ShouldLogUnexpectedErrorAndRethrow()
    {
        (TestPairWebExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new NullReferenceException());

        await Assert.ThrowsExactlyAsync<NullReferenceException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsFalse(
            logger.Entries.Any(entry => entry.EventId.Id == PairLoadFailedEventId),
            "An unexpected exception type must not be relabelled as a pair-load failure.");
        Assert.AreEqual(
            1,
            logger.Entries.Count(entry => entry.EventId.Id == PairLoadUnexpectedErrorEventId && entry.Level == LogLevel.Error),
            "An unexpected exception type must be logged once as an unexpected error before rethrowing.");
    }

    /// <summary>
    /// Verifies that a transport failure from the source is logged as a pair-load failure and rethrown unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenSourceThrowsHttpRequestException_ShouldLogFailureAndRethrow()
    {
        (TestPairWebExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new HttpRequestException("boom"));

        await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsTrue(logger.Entries.Any(entry => entry.EventId.Id == PairLoadFailedEventId));
    }

    /// <summary>
    /// Verifies that a malformed-data failure from the source is logged as a pair-load failure and rethrown unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenSourceThrowsFormatException_ShouldLogFailureAndRethrow()
    {
        (TestPairWebExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new ExchangeRateFormatException("boom"));

        await Assert.ThrowsExactlyAsync<ExchangeRateFormatException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsTrue(logger.Entries.Any(entry => entry.EventId.Id == PairLoadFailedEventId));
    }

    /// <summary>
    /// Verifies that a cancellation surfaced by the source propagates without being logged as a pair-load failure.
    /// </summary>
    [TestMethod]
    public async Task LoadPairAsync_WhenSourceThrowsOperationCanceledException_ShouldRethrowWithoutFailureLog()
    {
        (TestPairWebExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new OperationCanceledException());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await provider.LoadPairAsync("AUD", "USD", new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsFalse(logger.Entries.Any(entry => entry.EventId.Id is PairLoadFailedEventId or PairLoadUnexpectedErrorEventId));
    }
}
