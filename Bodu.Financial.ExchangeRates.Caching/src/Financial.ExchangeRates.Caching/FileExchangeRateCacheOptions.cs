// ---------------------------------------------------------------------------------------------------------------
// <copyright file="FileExchangeRateCacheOptions.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Configures a file-backed <see cref="IExchangeRateCache" />: the single provider inherited from
/// <see cref="ExchangeRateCacheOptions" /> together with the directory its cache files are written under.
/// </summary>
/// <remarks>
/// Expiry is not a storage concern — it is supplied per call by the caching provider — so this type carries only the
/// storage location in addition to the bound provider.
/// </remarks>
public class FileExchangeRateCacheOptions
    : ExchangeRateCacheOptions
{
    /// <summary>
    /// Gets or sets the directory used by the on-disk cache.
    /// </summary>
    /// <value>
    /// The cache directory, or <see langword="null" /> to use a <c>bodu-exchange-rates</c> folder under the system
    /// temporary path.
    /// </value>
    public string? CacheDirectory { get; set; }

    /// <summary>
    /// Gets or sets the layout describing where a currency pair's rows are stored: the folder hierarchy, the file name,
    /// and whether the rows are split across files by date.
    /// </summary>
    /// <value>
    /// The file layout. Defaults to <see cref="ExchangeRateCacheFileLayout.SingleFile" />, which stores a pair's whole
    /// history in one file under a per-provider folder.
    /// </value>
    /// <remarks>
    /// Use a built-in partitioned layout — <see cref="ExchangeRateCacheFileLayout.Yearly" />,
    /// <see cref="ExchangeRateCacheFileLayout.Monthly" />, or <see cref="ExchangeRateCacheFileLayout.Daily" /> — to
    /// split a pair's rows into per-period files, or <see cref="ExchangeRateCacheFileLayout.Create" /> to supply custom
    /// folder and file-name rules and a custom <see cref="ExchangeRateCachePartitionStrategy" />.
    /// </remarks>
    public ExchangeRateCacheFileLayout Layout { get; set; } = ExchangeRateCacheFileLayout.SingleFile;

    /// <inheritdoc />
    /// <exception cref="ArgumentException">Thrown when <see cref="Layout" /> is <see langword="null" />.</exception>
    public override void Validate()
    {
        base.Validate();

        if (Layout is null)
            throw new ArgumentException(CachingResourceStrings.Arg_Invalid_LayoutNull, nameof(Layout));
    }

    /// <inheritdoc />
    public override bool TryValidate(out string? error)
    {
        if (!base.TryValidate(out error))
            return false;

        if (Layout is null)
        {
            error = CachingResourceStrings.Arg_Invalid_LayoutNull;
            return false;
        }

        error = null;
        return true;
    }
}
