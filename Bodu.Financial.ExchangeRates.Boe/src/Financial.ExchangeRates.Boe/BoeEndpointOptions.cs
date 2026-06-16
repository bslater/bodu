// ---------------------------------------------------------------------------------------------------------------
// <copyright file="BoeEndpointOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Globalization;

namespace Bodu.Financial.ExchangeRates.Boe;

/// <summary>
/// Configures the provider's connection to the Bank of England Interactive Statistical Database (IADB): where the CSV
/// query endpoint lives and how the HTTP requests that drive it are shaped.
/// </summary>
/// <remarks>
/// <para>
/// This object isolates the network-facing settings from the behavioural options on
/// <see cref="BoeExchangeRateOptions" />, so the host, query path, transport timeout, and request identity can be
/// pointed at a mirror or proxy of the IADB — or simply tuned — without touching caching or series configuration. A
/// range request is composed from <see cref="BaseUrl" />, <see cref="QueryPath" />, the requested series codes, and the
/// inclusive date range through <see cref="BuildRequestUrl(IReadOnlyList{string}, DateOnly, DateOnly)" />.
/// </para>
/// <para>
/// Every member carries a working default, so the object binds cleanly through <c>Microsoft.Extensions.Options</c> and
/// requires no configuration for the common case.
/// </para>
/// </remarks>
public sealed class BoeEndpointOptions
{
    /// <summary>
    /// Gets or sets the base URL under which the IADB endpoints live. Should end with a trailing slash so the query
    /// path resolves as a relative path.
    /// </summary>
    /// <value>The base URL; defaults to the Bank of England IADB path.</value>
    public Uri BaseUrl { get; set; } = new("https://www.bankofengland.co.uk/boeapps/database/");

    /// <summary>
    /// Gets or sets the relative path of the IADB CSV column query endpoint.
    /// </summary>
    /// <value>The query path; defaults to <c>_iadb-fromshowcolumns.asp</c>.</value>
    public string QueryPath { get; set; } = "_iadb-fromshowcolumns.asp";

    /// <summary>
    /// Gets or sets the HTTP request timeout applied to range downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The HTTP timeout; defaults to 30 seconds.</value>
    public TimeSpan HttpTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets the <c>User-Agent</c> header applied to range downloads by the dependency-injection registration.
    /// </summary>
    /// <value>The user-agent string; defaults to a Bodu identifier.</value>
    public string UserAgent { get; set; } = "Bodu.Financial.ExchangeRates.Boe";

    /// <summary>
    /// Composes the absolute IADB CSV request URL for a set of series codes over an inclusive date range.
    /// </summary>
    /// <param name="seriesCodes">The IADB series codes to request.</param>
    /// <param name="startDate">The inclusive start of the range.</param>
    /// <param name="endDate">The inclusive end of the range.</param>
    /// <returns>The absolute request URL.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="seriesCodes" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="BaseUrl" /> has not been configured.
    /// </exception>
    public Uri BuildRequestUrl(IReadOnlyList<string> seriesCodes, DateOnly startDate, DateOnly endDate)
    {
        ThrowHelper.ThrowIfNull(seriesCodes);
        if (BaseUrl is null)
            throw new InvalidOperationException(BoeResourceStrings.Op_Invalid_BoeEndpointBaseUrl);

        string codes = string.Join(",", seriesCodes);
        string query =
            "?csv.x=yes" +
            "&Datefrom=" + Uri.EscapeDataString(FormatDate(startDate)) +
            "&Dateto=" + Uri.EscapeDataString(FormatDate(endDate)) +
            "&SeriesCodes=" + Uri.EscapeDataString(codes) +
            "&CSVF=TN&UsingCodes=Y&VPD=Y&VFD=N";

        return new Uri(BaseUrl, QueryPath + query);
    }

    /// <summary>
    /// Validates the endpoint options, throwing when a required value is missing or an invariant is violated.
    /// </summary>
    /// <exception cref="ArgumentException">
    /// Thrown when <see cref="BaseUrl" /> is <see langword="null" />; <see cref="QueryPath" /> is empty or white space;
    /// or <see cref="HttpTimeout" /> is not greater than zero.
    /// </exception>
    public void Validate()
    {
        if (!TryValidate(out string? error))
            throw new ArgumentException(error);
    }

    /// <summary>
    /// Attempts to validate the endpoint options without throwing, reporting the first invariant that is violated.
    /// </summary>
    /// <param name="error">
    /// When this method returns <see langword="false" />, a message describing the first violated invariant; otherwise
    /// <see langword="null" />.
    /// </param>
    /// <returns><see langword="true" /> when every invariant holds; otherwise <see langword="false" />.</returns>
    public bool TryValidate(out string? error)
    {
        if (BaseUrl is null)
        {
            error = BoeResourceStrings.Arg_Invalid_BoeEndpointBaseUrl;
            return false;
        }

        if (string.IsNullOrWhiteSpace(QueryPath))
        {
            error = BoeResourceStrings.Arg_Invalid_BoeEndpointQueryPath;
            return false;
        }

        if (HttpTimeout <= TimeSpan.Zero)
        {
            error = BoeResourceStrings.Arg_Invalid_BoeEndpointHttpTimeout;
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Formats a date in the <c>dd/MMM/yyyy</c> form the IADB query parameters expect.
    /// </summary>
    /// <param name="date">The date to format.</param>
    /// <returns>The formatted date.</returns>
    private static string FormatDate(DateOnly date) =>
        date.ToString("dd/MMM/yyyy", CultureInfo.InvariantCulture);
}
