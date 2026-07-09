// ---------------------------------------------------------------------------------------------------------------
// <copyright file="RbaRateProviderTests.LoadFailureLogging.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace Bodu.Financial.ExchangeRates;

public partial class RbaRateProviderTests
{
    /// <summary>The event id of the <c>EraLoadFailed</c> diagnostic emitted when an era download fails.</summary>
    private const int EraLoadFailedEventId = 4303;

    /// <summary>The event id of the <c>EraLoadUnexpectedError</c> diagnostic emitted when an era download throws an unexpected exception type.</summary>
    private const int EraLoadUnexpectedErrorEventId = 4306;

    /// <summary>
    /// Creates a provider whose source throws the supplied exception, capturing its log output.
    /// </summary>
    /// <param name="exception">The exception the source throws from every fetch.</param>
    /// <returns>The provider and its capturing logger.</returns>
    private static (RbaRateProvider Provider, CapturingLogger Logger) CreateThrowing(Exception exception)
    {
        RbaRateProviderOptions options = new() { AllowSynchronousNetworkAccess = false, EnableDiskCache = false };
        CapturingLogger logger = new();

        return (new RbaRateProvider(new ThrowingRbaRateTableSource(exception), options, logger), logger);
    }

    /// <summary>
    /// Verifies that an unexpected exception type thrown by the source — indicating a provider bug rather than a
    /// transport or data failure — is logged as a distinct unexpected error at <see cref="LogLevel.Error" /> and
    /// rethrown, without being relabelled as an ordinary era-load failure.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsUnexpectedException_ShouldLogUnexpectedErrorAndRethrow()
    {
        (RbaRateProvider provider, CapturingLogger logger) = CreateThrowing(new NullReferenceException());

        await Assert.ThrowsExactlyAsync<NullReferenceException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsFalse(
            logger.Entries.Any(entry => entry.EventId.Id == EraLoadFailedEventId),
            "An unexpected exception type must not be relabelled as an era-load failure.");
        Assert.AreEqual(
            1,
            logger.Entries.Count(entry => entry.EventId.Id == EraLoadUnexpectedErrorEventId && entry.Level == LogLevel.Error),
            "An unexpected exception type must be logged once as an unexpected error before rethrowing.");
    }

    /// <summary>
    /// Verifies that a transport failure from the source is logged as an era-load failure and rethrown unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsHttpRequestException_ShouldLogFailureAndRethrow()
    {
        (RbaRateProvider provider, CapturingLogger logger) = CreateThrowing(new HttpRequestException("boom"));

        await Assert.ThrowsExactlyAsync<HttpRequestException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsTrue(logger.Entries.Any(entry => entry.EventId.Id == EraLoadFailedEventId));
    }

    /// <summary>
    /// Verifies that a malformed-data failure from the source is logged as an era-load failure and rethrown unchanged.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsFormatException_ShouldLogFailureAndRethrow()
    {
        (RbaRateProvider provider, CapturingLogger logger) = CreateThrowing(new ExchangeRateFormatException("boom"));

        await Assert.ThrowsExactlyAsync<ExchangeRateFormatException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsTrue(logger.Entries.Any(entry => entry.EventId.Id == EraLoadFailedEventId));
    }

    /// <summary>
    /// Verifies that a cancellation surfaced by the source propagates without being logged as an era-load failure.
    /// </summary>
    [TestMethod]
    public async Task LoadRangeAsync_WhenSourceThrowsOperationCanceledException_ShouldRethrowWithoutFailureLog()
    {
        (RbaRateProvider provider, CapturingLogger logger) = CreateThrowing(new OperationCanceledException());

        await Assert.ThrowsExactlyAsync<OperationCanceledException>(async () =>
        {
            await provider.LoadRangeAsync(new DateOnly(2023, 1, 1), new DateOnly(2023, 1, 31));
        });

        Assert.IsFalse(logger.Entries.Any(entry => entry.EventId.Id is EraLoadFailedEventId or EraLoadUnexpectedErrorEventId));
    }
}
