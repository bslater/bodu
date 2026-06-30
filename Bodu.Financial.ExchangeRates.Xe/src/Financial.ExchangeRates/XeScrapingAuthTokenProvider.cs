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
/// Acquires the <c>Authorization: Basic</c> token the XE.com charting-rates endpoint requires by scanning the script
/// chunks the XE website publishes, then caches it for reuse.
/// </summary>
/// <remarks>
/// <para>
/// Acquisition fetches the bootstrap page, harvests the <c>_next</c> script chunks it references, and scans each for
/// the credential. The credential is built inside a <c>btoa(...)</c> call adjacent to a <c>Basic </c> literal — for
/// example <c>e.set("Authorization", `Basic ${btoa("user:secret")}`)</c>; the string literals passed to <c>btoa</c> are
/// concatenated and base64-encoded to form the token. The first chunk that yields a credential wins. When no referenced
/// chunk matches, the webpack runtime chunk's identifier-to-hash map is used to reconstruct lazily-loaded chunk URLs,
/// which are then scanned as a fallback.
/// </para>
/// <para>
/// The acquired token is cached and shared by all callers; concurrent acquisitions are coalesced behind a gate so the
/// scan runs once even under contention. A caller that observes a rejected token requests a refresh, which re-scans
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

    /// <summary>The maximum number of characters allowed between a <c>Basic </c> marker and the <c>btoa(</c> that builds the credential.</summary>
    private const int BasicToBtoaWindow = 16;

    /// <summary>The upper bound on the number of script chunks scanned before the scan gives up.</summary>
    private const int MaxChunksScanned = 400;

    /// <summary>The HTTP client used to download the bootstrap page and the script chunks.</summary>
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
    /// <param name="httpClient">The HTTP client used to download the bootstrap page and the script chunks.</param>
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
            // scan runs once under contention. A forced refresh drops the stale token first so a concurrent
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
        // the lock. Without this, a synchronously-completing scan would run its finally (clearing _inFlight) before the
        // assignment, leaving a stale completed task in the slot that a later refresh would join instead of re-scanning.
        await Task.Yield();

        try
        {
            // The shared acquisition is decoupled from any one caller's token, so a single caller's cancellation
            // cannot abandon the scan the others are waiting on.
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
    /// Scans the XE website's script chunks for the credential, returning the first one found.
    /// </summary>
    /// <param name="cancellationToken">A token to observe while awaiting the downloads.</param>
    /// <returns>A task that yields the base64 credential.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the credential cannot be located in any scanned chunk.
    /// </exception>
    private async Task<string> AcquireTokenAsync(CancellationToken cancellationToken)
    {
        string bootstrap = await _httpClient.GetStringAsync(_options.AuthBootstrapUrl, cancellationToken).ConfigureAwait(false);

        List<Uri> chunkUrls = HarvestChunkUrls(bootstrap, _options.AuthBootstrapUrl);
        HashSet<string> seen = new(StringComparer.Ordinal);
        int scanned = 0;

        // Primary: scan the chunks the bootstrap page references directly. The credential lives in the always-loaded
        // _app entry chunk, which HarvestChunkUrls orders first, so the scan typically returns after a single chunk.
        foreach (Uri chunkUrl in chunkUrls)
        {
            if (scanned >= MaxChunksScanned)
                break;

            if (!seen.Add(chunkUrl.AbsoluteUri))
                continue;

            scanned++;
            string chunk = await TryGetStringAsync(chunkUrl, cancellationToken).ConfigureAwait(false);
            if (TryExtractToken(chunk, out string token))
                return token;
        }

        // Fallback: reconstruct lazily-loaded chunk URLs from the webpack runtime chunk's identifier-to-hash map and
        // scan those. Only reached when no bootstrap-referenced chunk carried the credential.
        Uri? runtimeUrl = chunkUrls.FirstOrDefault(static url => RuntimeChunkRegex().IsMatch(url.AbsolutePath));
        if (runtimeUrl is not null)
        {
            string runtime = await TryGetStringAsync(runtimeUrl, cancellationToken).ConfigureAwait(false);

            foreach (Uri chunkUrl in ReconstructLazyChunkUrls(runtime, _options.AuthScriptBaseUrl))
            {
                if (scanned >= MaxChunksScanned)
                    break;

                if (!seen.Add(chunkUrl.AbsoluteUri))
                    continue;

                scanned++;
                string chunk = await TryGetStringAsync(chunkUrl, cancellationToken).ConfigureAwait(false);
                if (TryExtractToken(chunk, out string token))
                    return token;
            }
        }

        throw TokenUnavailable(scanned);
    }

    /// <summary>
    /// Downloads a script chunk, returning an empty string when the request fails so the scan can continue.
    /// </summary>
    /// <param name="url">The absolute chunk URL.</param>
    /// <param name="cancellationToken">A token to observe while awaiting the download.</param>
    /// <returns>A task that yields the chunk text, or an empty string when the download fails.</returns>
    private async Task<string> TryGetStringAsync(Uri url, CancellationToken cancellationToken)
    {
        try
        {
            return await _httpClient.GetStringAsync(url, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException)
        {
            // A reconstructed lazy-chunk URL that no longer exists 404s; skip it and keep scanning the rest.
            return string.Empty;
        }
    }

    /// <summary>
    /// Harvests the absolute <c>_next</c> script chunk URLs referenced by the bootstrap page, ordered so the
    /// always-loaded <c>_app</c> entry chunk — where the credential lives — is scanned first.
    /// </summary>
    /// <param name="html">The bootstrap page markup.</param>
    /// <param name="baseUri">The URL the referenced chunk paths are resolved against.</param>
    /// <returns>The ordered, deduplicated chunk URLs.</returns>
    private static List<Uri> HarvestChunkUrls(string html, Uri baseUri)
    {
        List<Uri> urls = new();
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (Match match in ChunkUrlRegex().Matches(html))
        {
            if (Uri.TryCreate(baseUri, match.Value, out Uri? resolved) && seen.Add(resolved.AbsoluteUri))
                urls.Add(resolved);
        }

        // OrderBy is a stable sort, so the original document order is preserved within each group.
        return urls
            .OrderBy(static url => url.AbsolutePath.Contains("/pages/_app", StringComparison.Ordinal) ? 0 : 1)
            .ToList();
    }

    /// <summary>
    /// Reconstructs lazily-loaded chunk URLs from the webpack runtime chunk's identifier-to-hash map.
    /// </summary>
    /// <param name="runtimeJs">The webpack runtime chunk text.</param>
    /// <param name="scriptBase">
    /// The base URL the reconstructed <c>static/chunks/…</c> names are resolved against.
    /// </param>
    /// <returns>The deduplicated reconstructed chunk URLs.</returns>
    private static List<Uri> ReconstructLazyChunkUrls(string runtimeJs, Uri scriptBase)
    {
        List<Uri> urls = new();
        HashSet<string> seen = new(StringComparer.Ordinal);

        foreach (Match match in IdHashRegex().Matches(runtimeJs))
        {
            string relative = string.Format(
                CultureInfo.InvariantCulture,
                "static/chunks/{0}.{1}.js",
                match.Groups[1].Value,
                match.Groups[2].Value);

            if (Uri.TryCreate(scriptBase, relative, out Uri? resolved) && seen.Add(resolved.AbsoluteUri))
                urls.Add(resolved);
        }

        return urls;
    }

    /// <summary>
    /// Attempts to extract and base64-encode the authorization credential from a script chunk.
    /// </summary>
    /// <param name="chunk">The downloaded script chunk.</param>
    /// <param name="token">
    /// When this method returns <see langword="true" />, the base64 credential; otherwise an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the chunk yields a credential; otherwise <see langword="false" />.
    /// </returns>
    private static bool TryExtractToken(string chunk, out string token)
    {
        token = string.Empty;
        if (chunk.Length == 0)
            return false;

        int cursor = 0;
        while (true)
        {
            int basicIndex = chunk.IndexOf("Basic ", cursor, StringComparison.Ordinal);
            if (basicIndex < 0)
                return false;

            // The credential is built by a btoa(...) call immediately after the "Basic " marker. Require it within a
            // short window so an unrelated "Basic " literal elsewhere in the chunk is not mistaken for the credential.
            int btoaIndex = chunk.IndexOf("btoa(", basicIndex, StringComparison.Ordinal);
            if (btoaIndex >= 0 && btoaIndex - basicIndex <= BasicToBtoaWindow && TryDecodeBtoa(chunk, btoaIndex, out token))
                return true;

            cursor = basicIndex + 1;
        }
    }

    /// <summary>
    /// Decodes the credential carried by a <c>btoa(...)</c> call by concatenating its quoted string literals and
    /// base64-encoding the result.
    /// </summary>
    /// <param name="chunk">The script chunk containing the call.</param>
    /// <param name="btoaIndex">The index of the <c>btoa</c> token.</param>
    /// <param name="token">
    /// When this method returns <see langword="true" />, the base64 credential; otherwise an empty string.
    /// </param>
    /// <returns>
    /// <see langword="true" /> when the call balances and carries a non-empty credential; otherwise
    /// <see langword="false" />.
    /// </returns>
    private static bool TryDecodeBtoa(string chunk, int btoaIndex, out string token)
    {
        token = string.Empty;

        // Walk forward from the opening parenthesis of the btoa(...) call, tracking nesting depth, until the matching
        // closing parenthesis balances the expression.
        int depth = 1;
        int length = BtoaArgumentOffset;
        while (depth != 0)
        {
            int index = btoaIndex + length++;
            if (index >= chunk.Length)
                return false;

            switch (chunk[index])
            {
                case '(': depth++; break;
                case ')': depth--; break;
            }
        }

        string expression = chunk.Substring(btoaIndex, length);
        string credential = string.Concat(QuotedLiteralRegex().Matches(expression).Select(match => match.Groups["a"].Value));
        if (credential.Length == 0)
            return false;

        token = Convert.ToBase64String(Encoding.ASCII.GetBytes(credential));
        return true;
    }

    /// <summary>
    /// Builds the exception thrown when the token cannot be acquired.
    /// </summary>
    /// <param name="scannedChunks">The number of chunks scanned before the scan gave up.</param>
    /// <returns>The exception to throw.</returns>
    private static InvalidOperationException TokenUnavailable(int scannedChunks) =>
        new(string.Format(CultureInfo.CurrentCulture, XeResourceStrings.Op_Invalid_XeAuthTokenUnavailable, scannedChunks));

    /// <summary>
    /// Gets the compiled expression matching an absolute <c>_next</c> script chunk path referenced by the bootstrap
    /// page.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("/_next/static/chunks/[^\"'()\\s]+\\.js")]
    private static partial Regex ChunkUrlRegex();

    /// <summary>
    /// Gets the compiled expression matching the webpack runtime chunk path.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("/webpack-[^/]+\\.js$")]
    private static partial Regex RuntimeChunkRegex();

    /// <summary>
    /// Gets the compiled expression matching a webpack chunk identifier-to-hash pair, capturing the numeric identifier
    /// as group 1 and the hash as group 2.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("(\\d+):\"([0-9a-f]{6,})\"")]
    private static partial Regex IdHashRegex();

    /// <summary>
    /// Gets the compiled expression matching a double-quoted string literal, capturing its inner text as group <c>a</c>.
    /// </summary>
    /// <returns>The compiled regular expression.</returns>
    [GeneratedRegex("\"(?<a>[^\"]*)\"")]
    private static partial Regex QuotedLiteralRegex();
}
