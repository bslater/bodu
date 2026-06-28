// ---------------------------------------------------------------------------------------------------------------
// <copyright file="XeScrapingAuthTokenProvider.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Bodu.Financial.ExchangeRates;

/// <summary>
/// Acquires the <c>Authorization: Basic</c> token the XE.com charting-rates endpoint requires by inspecting the XE
/// website's published application script bundle, then caches it for reuse.
/// </summary>
/// <remarks>
/// <para>
/// Acquisition is a two-step scrape: the bootstrap page names the current application script chunk, and that chunk
/// embeds the credential inside a <c>btoa(...)</c> call adjacent to the literal <c>"Authorization","Basic "</c>. The
/// string literals passed to <c>btoa</c> are concatenated and base64-encoded to form the token.
/// </para>
/// <para>
/// The acquired token is cached and shared by all callers; concurrent acquisitions are coalesced behind a gate so the
/// scrape runs once even under contention. A caller that observes a rejected token requests a refresh, which re-scrapes
/// unless another caller has already replaced the token in the meantime.
/// </para>
/// <para>
/// This strategy depends on the XE website's current structure and is therefore inherently brittle; it carries no
/// affiliation with or endorsement by XE.
/// </para>
/// </remarks>
internal sealed partial class XeScrapingAuthTokenProvider
    : IXeAuthTokenProvider
{
    /// <summary>The number of characters past the located <c>btoa</c> token at which the argument list opens.</summary>
    private const int BtoaArgumentOffset = 5;

    /// <summary>The HTTP client used to download the bootstrap page and the application script chunk.</summary>
    private readonly HttpClient _httpClient;

    /// <summary>The provider options supplying the token-acquisition URLs.</summary>
    private readonly XeExchangeRateOptions _options;

    /// <summary>Guards <see cref="_token" /> and <see cref="_inFlight" /> against concurrent mutation.</summary>
    private readonly object _sync = new();

    /// <summary>The acquisition currently in flight, or <see langword="null" /> when none is running.</summary>
    private Task<string>? _inFlight;

    /// <summary>The cached token, or <see langword="null" /> before the first acquisition.</summary>
    private string? _token;

    /// <summary>
    /// Initializes a new instance of the <see cref="XeScrapingAuthTokenProvider" /> class.
    /// </summary>
    /// <param name="httpClient">
    /// The HTTP client used to download the bootstrap page and the application script chunk.
    /// </param>
    /// <param name="options">The provider options.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="httpClient" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    internal XeScrapingAuthTokenProvider(HttpClient httpClient, XeExchangeRateOptions options)
    {
        ThrowHelper.ThrowIfNull(httpClient);
        ThrowHelper.ThrowIfNull(options);

        _httpClient = httpClient;
        _options = options;
    }

    /// <inheritdoc />
    public ValueTask<string> GetTokenAsync(bool forceRefresh, CancellationToken cancellationToken = default)
    {
        Task<string> acquisition;

        lock (_sync)
        {
            // Reuse a usable cached token unless the caller explicitly asks for a refresh.
            string? current = _token;
            if (current is not null && !forceRefresh)
                return new ValueTask<string>(current);

            // Coalesce: when an acquisition (initial or refresh) is already running, every caller joins it so the
            // scrape runs once under contention. A forced refresh drops the stale token first so a concurrent
            // non-refresh caller cannot read it back.
            if (_inFlight is null)
            {
                if (forceRefresh)
                    _token = null;

                _inFlight = AcquireAndCacheAsync();
            }

            acquisition = _inFlight;
        }

        // Each caller observes its own cancellation while the shared acquisition keeps running for the others.
        return new ValueTask<string>(acquisition.WaitAsync(cancellationToken));
    }

    /// <summary>
    /// Runs the shared acquisition, caches its result, and clears the in-flight slot so a later refresh can start anew.
    /// </summary>
    /// <returns>A task that yields the base64 credential.</returns>
    private async Task<string> AcquireAndCacheAsync()
    {
        // Suspend before doing any work so this task is always incomplete when the caller assigns it to _inFlight under
        // the lock. Without this, a synchronously-completing scrape would run its finally (clearing _inFlight) before the
        // assignment, leaving a stale completed task in the slot that a later refresh would join instead of re-scraping.
        await Task.Yield();

        try
        {
            // The shared acquisition is decoupled from any one caller's token, so a single caller's cancellation
            // cannot abandon the scrape the others are waiting on.
            string token = await AcquireTokenAsync(CancellationToken.None).ConfigureAwait(false);

            lock (_sync)
            {
                _token = token;
            }

            return token;
        }
        finally
        {
            lock (_sync)
            {
                _inFlight = null;
            }
        }
    }

    /// <summary>
    /// Performs the two-step scrape that discovers and encodes the authorization token.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while awaiting the downloads.</param>
    /// <returns>A task that yields the base64 credential.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the token cannot be located in the XE website's script bundle.
    /// </exception>
    private async Task<string> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        string bootstrap = await _httpClient.GetStringAsync(_options.AuthBootstrapUrl, cancellationToken).ConfigureAwait(false);

        Match scriptMatch = AppChunkRegex().Match(bootstrap);
        if (!scriptMatch.Success)
            throw TokenUnavailable();

        Uri chunkUrl = new(_options.AuthChunkBaseUrl, scriptMatch.Value);
        string chunk = await _httpClient.GetStringAsync(chunkUrl, cancellationToken).ConfigureAwait(false);

        return ExtractToken(chunk);
    }

    /// <summary>
    /// Extracts and base64-encodes the authorization credential from the application script chunk.
    /// </summary>
    /// <param name="chunk">The downloaded application script chunk.</param>
    /// <returns>The base64 credential.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the <c>btoa(...)</c> credential expression cannot be located or balanced.
    /// </exception>
    private static string ExtractToken(string chunk)
    {
        int anchor = chunk.IndexOf("\"Authorization\",\"Basic \"", StringComparison.Ordinal);
        if (anchor < 0)
            throw TokenUnavailable();

        int btoa = chunk.IndexOf("btoa", anchor, StringComparison.Ordinal);
        if (btoa < 0)
            throw TokenUnavailable();

        // Walk forward from the opening parenthesis of the btoa(...) call, tracking nesting depth, until the matching
        // closing parenthesis balances the expression.
        int depth = 1;
        int length = BtoaArgumentOffset;
        while (depth != 0)
        {
            int index = btoa + length++;
            if (index >= chunk.Length)
                throw TokenUnavailable();

            switch (chunk[index])
            {
                case '(': depth++; break;
                case ')': depth--; break;
            }
        }

        string expression = chunk.Substring(btoa, length);
        string credential = string.Concat(QuotedLiteralRegex().Matches(expression).Select(match => match.Groups["a"].Value));
        if (credential.Length == 0)
            throw TokenUnavailable();

        return Convert.ToBase64String(Encoding.ASCII.GetBytes(credential));
    }

    /// <summary>
    /// Builds the exception thrown when the token cannot be acquired.
    /// </summary>
    /// <returns>The exception to throw.</returns>
    private static InvalidOperationException TokenUnavailable() =>
        new(XeResourceStrings.Op_Invalid_XeAuthTokenUnavailable);

    /// <summary>
    /// Gets the compiled expression matching the XE application script chunk file name in the bootstrap page.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("_app-[a-zA-Z0-9]{16}\\.js")]
    private static partial Regex AppChunkRegex();

    /// <summary>
    /// Gets the compiled expression matching a double-quoted string literal, capturing its inner text as group <c>a</c>.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("\"(?<a>[^\"]*)\"")]
    private static partial Regex QuotedLiteralRegex();
}
