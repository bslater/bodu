// ---------------------------------------------------------------------------------------------------------------
// <copyright file="PairWebExchangeRateProviderTestDoubles.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial;

/// <summary>
/// A concrete <see cref="WebExchangeRateProviderOptions" /> used by the shared-abstraction tests, with a toggle that
/// forces <see cref="TryValidateCore" /> to fail so the delegation can be observed.
/// </summary>
internal sealed class TestWebExchangeRateProviderOptions
    : WebExchangeRateProviderOptions
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestWebExchangeRateProviderOptions" /> class with a placeholder
    /// base address.
    /// </summary>
    public TestWebExchangeRateProviderOptions()
    {
        BaseAddress = new Uri("https://test.local/");
    }

    /// <summary>
    /// Gets or sets a value indicating whether the source-specific validation reports success.
    /// </summary>
    /// <value><see langword="true" /> when <see cref="TryValidateCore" /> reports success; otherwise <see langword="false" />.</value>
    public bool CoreValid { get; set; } = true;

    /// <inheritdoc />
    protected override bool TryValidateCore(out string? error)
    {
        if (!CoreValid)
        {
            error = "core invalid";
            return false;
        }

        error = null;
        return true;
    }
}

/// <summary>
/// A fixture-backed <see cref="IExchangeRatePairSource{TSeries}" /> that returns a fixed set of observations restricted
/// to each request's range and records how many times it was invoked.
/// </summary>
internal sealed class StubPairSource
    : IExchangeRatePairSource<string>
{
    /// <summary>The full set of observations the source can serve.</summary>
    private readonly IReadOnlyList<ExchangeRateObservation> _observations;

    /// <summary>
    /// Initializes a new instance of the <see cref="StubPairSource" /> class.
    /// </summary>
    /// <param name="observations">The observations the source serves, before range restriction.</param>
    public StubPairSource(params ExchangeRateObservation[] observations)
    {
        _observations = observations;
    }

    /// <summary>
    /// Gets the number of times <see cref="GetPairAsync" /> has been invoked.
    /// </summary>
    /// <value>The invocation count.</value>
    public int CallCount { get; private set; }

    /// <inheritdoc />
    public ValueTask<PairRateData<string>> GetPairAsync(ExchangeRatePairRequest request, CancellationToken cancellationToken = default)
    {
        CallCount++;

        var inRange = _observations
            .Where(observation => observation.Date >= request.StartDate && observation.Date <= request.EndDate)
            .ToList();

        string series = $"{request.Pair.From}/{request.Pair.To}";
        return ValueTask.FromResult(new PairRateData<string>(request.Pair, inRange, series));
    }
}

/// <summary>
/// A minimal <see cref="PairWebExchangeRateProvider{TSeries}" /> over a <see cref="StubPairSource" /> for exercising the
/// shared base behaviour directly.
/// </summary>
internal sealed class TestPairWebExchangeRateProvider
    : PairWebExchangeRateProvider<string>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TestPairWebExchangeRateProvider" /> class.
    /// </summary>
    /// <param name="source">The pair source.</param>
    /// <param name="options">The provider options.</param>
    public TestPairWebExchangeRateProvider(IExchangeRatePairSource<string> source, WebExchangeRateProviderOptions options)
        : base(source, options, logger: null, ownedHttpClient: null, timeProvider: null)
    {
    }

    /// <inheritdoc />
    protected override string ProviderId => "TEST";
}
