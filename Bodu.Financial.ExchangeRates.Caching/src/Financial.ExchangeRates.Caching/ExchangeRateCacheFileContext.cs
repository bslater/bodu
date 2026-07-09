// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheFileContext.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Supplies the inputs a file-name resolver needs to name a single cache file: the bound provider, the currency pair,
/// the partition key, and the format's file extension.
/// </summary>
/// <remarks>
/// Passed to the file-name delegate of an <see cref="ExchangeRateCacheFileLayout" />. For a single-file layout the
/// <see cref="PartitionKey" /> is the empty string; for a partitioned layout it is the partition's key (for example
/// <c>2023-01</c> for a monthly partition).
/// </remarks>
public readonly record struct ExchangeRateCacheFileContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateCacheFileContext" /> class. Initializes a new instance
    /// of the <see cref="ExchangeRateCacheFileContext" /> struct.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="pair">The currency pair the file holds.</param>
    /// <param name="partitionKey">The partition key, or the empty string for a single-file layout.</param>
    /// <param name="fileExtension">
    /// The file extension, including the leading period, applied by the serialization format.
    /// </param>
    public ExchangeRateCacheFileContext(string provider, CurrencyPair pair, string partitionKey, string fileExtension)
    {
        Provider = provider;
        Pair = pair;
        PartitionKey = partitionKey;
        FileExtension = fileExtension;
    }

    /// <summary>
    /// Gets the provider the cache is bound to.
    /// </summary>
    /// <value>The provider identifier.</value>
    public string Provider { get; }

    /// <summary>
    /// Gets the currency pair the file holds.
    /// </summary>
    /// <value>The currency pair.</value>
    public CurrencyPair Pair { get; }

    /// <summary>
    /// Gets the partition key the file holds.
    /// </summary>
    /// <value>The partition key, or the empty string for a single-file layout.</value>
    public string PartitionKey { get; }

    /// <summary>
    /// Gets the file extension, including the leading period, applied by the serialization format.
    /// </summary>
    /// <value>The file extension, for example <c>.toml</c>.</value>
    public string FileExtension { get; }
}
