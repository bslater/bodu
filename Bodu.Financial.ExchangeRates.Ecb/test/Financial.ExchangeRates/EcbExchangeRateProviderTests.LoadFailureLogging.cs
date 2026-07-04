// ---------------------------------------------------------------------------------------------------------------
// <copyright file="EcbExchangeRateProviderTests.LoadFailureLogging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net.Http;

namespace Bodu.Financial.ExchangeRates;

public partial class EcbExchangeRateProviderTests
{
    /// <summary>The event id of the <c>FeedLoadFailed</c> diagnostic emitted when a range download fails.</summary>
    private const int FeedLoadFailedEventId = 4103;

    /// <summary>
    /// Creates a provider whose source throws the supplied exception, capturing its log output.
    /// </summary>
    /// <param name="exception">The exception the source throws from every fetch.</param>
    /// <returns>The provider and its capturing logger.</returns>
    private static (EcbExchangeRateProvider Provider, CapturingLogger Logger) CreateThrowing(Exception exception)
    {
        EcbExchangeRateOptions options = new() { AllowSynchronousNetworkAccess = false, EnableDiskCache = false };
        CapturingLogger logger = new();

        return (new EcbExchangeRateProvider(new ThrowingEcbExchangeRateTableSource(exception), options, logger), logger);
    }

    /// <summary>
    /// Verifies that an unexpected exception type thrown by the source — indicating a provider bug rather than a
    /// transport or data failure — propagates without being logged as a feed-load failure.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsUnexpectedException_ShouldPropagateWithoutFailureLog()
    {
        (EcbExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new NullReferenceException());

        await Assert.ThrowsExactlyAsync<NullReferenceException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsFalse(
            logger.Entries.Any(entry => entry.EventId.Id == FeedLoadFailedEventId),
            "An unexpected exception type must not be relabelled as a feed-load failure.");
    }

    /// <summary>
    /// Verifies that a transport failure from the source is logged as a feed-load failure and rethrown unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsHttpRequestException_ShouldLogFailureAndRethrow()
    {
        (EcbExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new HttpRequestException("boom"));

        await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsTrue(logger.Entries.Any(entry => entry.EventId.Id == FeedLoadFailedEventId));
    }

    /// <summary>
    /// Verifies that a malformed-data failure from the source is logged as a feed-load failure and rethrown unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsFormatException_ShouldLogFailureAndRethrow()
    {
        (EcbExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new ExchangeRateFormatException("boom"));

        await Assert.ThrowsExactlyAsync<ExchangeRateFormatException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsTrue(logger.Entries.Any(entry => entry.EventId.Id == FeedLoadFailedEventId));
    }

    /// <summary>
    /// Verifies that a cancellation surfaced by the source propagates without being logged as a feed-load failure.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsOperationCanceledException_ShouldRethrowWithoutFailureLog()
    {
        (EcbExchangeRateProvider provider, CapturingLogger logger) = CreateThrowing(new OperationCanceledException());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsFalse(logger.Entries.Any(entry => entry.EventId.Id == FeedLoadFailedEventId));
    }
}
