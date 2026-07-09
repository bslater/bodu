// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ExchangeRateCacheFileLayout.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Financial.ExchangeRates.Caching;

/// <summary>
/// Describes where a file-backed exchange-rate cache stores a currency pair's rows: the folder that holds the pair's
/// file or files, the name of each file, and — through its <see cref="PartitionStrategy" /> — whether the rows are
/// split across files by date.
/// </summary>
/// <remarks>
/// <para>
/// A layout combines three decisions: the <see cref="PartitionStrategy" /> (single file, or one file per year, month,
/// day, or custom period), the directory each pair's files live in, and the name of each file. The built-in
/// <see cref="SingleFile" /> layout reproduces the default
/// <c>&lt;root&gt;/&lt;provider&gt;/&lt;From&gt;&lt;To&gt;.ext</c> layout; <see cref="Yearly" />,
/// <see cref="Monthly" />, and <see cref="Daily" /> isolate each pair in its own folder and name the per-period files
/// by partition key, for example <c>&lt;root&gt;/&lt;provider&gt;/AUDUSD/2023-01.toml</c>.
/// </para>
/// <para>
/// Use <see cref="Create" /> to build a custom layout from a partition strategy and optional directory and file-name
/// delegates. A partitioned layout must isolate each pair's files in a directory that holds no other pair, because the
/// cache discovers a pair's partitions by enumerating that directory; a custom directory delegate that shares a folder
/// across pairs must therefore encode the pair in the file name to keep them distinct.
/// </para>
/// </remarks>
public sealed class ExchangeRateCacheFileLayout
{
    /// <summary>The cached set of characters that are not permitted in a file-name segment.</summary>
    private static readonly char[] s_invalidFileNameChars = Path.GetInvalidFileNameChars();

    /// <summary>The delegate resolving the directory that holds a pair's file or files.</summary>
    private readonly Func<ExchangeRateCacheDirectoryContext, string> _directory;

    /// <summary>The delegate resolving the name of a single cache file.</summary>
    private readonly Func<ExchangeRateCacheFileContext, string> _fileName;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExchangeRateCacheFileLayout" /> class.
    /// </summary>
    /// <param name="partitionStrategy">The strategy deciding how a pair is split across files by date.</param>
    /// <param name="directory">The delegate resolving the directory that holds a pair's file or files.</param>
    /// <param name="fileName">The delegate resolving the name of a single cache file.</param>
    private ExchangeRateCacheFileLayout(
        ExchangeRateCachePartitionStrategy partitionStrategy,
        Func<ExchangeRateCacheDirectoryContext, string> directory,
        Func<ExchangeRateCacheFileContext, string> fileName)
    {
        PartitionStrategy = partitionStrategy;
        _directory = directory;
        _fileName = fileName;
    }

    /// <summary>
    /// Gets the layout that stores a pair's whole history in one file under a per-provider folder, reproducing the
    /// default <c>&lt;root&gt;/&lt;provider&gt;/&lt;From&gt;&lt;To&gt;.ext</c> layout.
    /// </summary>
    /// <value>The single-file layout.</value>
    public static ExchangeRateCacheFileLayout SingleFile { get; } =
        new(
            ExchangeRateCachePartitionStrategy.Single,
            static ctx => Path.Combine(ctx.Root, Sanitize(ctx.Provider)),
            static ctx => Sanitize(PairToken(ctx.Pair)) + ctx.FileExtension);

    /// <summary>
    /// Gets the layout that isolates each pair in its own folder and splits its rows into one file per calendar year.
    /// </summary>
    /// <value>The yearly partitioned layout.</value>
    public static ExchangeRateCacheFileLayout Yearly { get; } =
        Partitioned(ExchangeRateCachePartitionStrategy.Yearly);

    /// <summary>
    /// Gets the layout that isolates each pair in its own folder and splits its rows into one file per calendar month.
    /// </summary>
    /// <value>The monthly partitioned layout.</value>
    public static ExchangeRateCacheFileLayout Monthly { get; } =
        Partitioned(ExchangeRateCachePartitionStrategy.Monthly);

    /// <summary>
    /// Gets the layout that isolates each pair in its own folder and splits its rows into one file per calendar day.
    /// </summary>
    /// <value>The daily partitioned layout.</value>
    public static ExchangeRateCacheFileLayout Daily { get; } =
        Partitioned(ExchangeRateCachePartitionStrategy.Daily);

    /// <summary>
    /// Gets the strategy deciding how a pair's rows are split across files by date.
    /// </summary>
    /// <value>
    /// The partition strategy; <see cref="ExchangeRateCachePartitionStrategy.Single" /> for a single-file layout.
    /// </value>
    public ExchangeRateCachePartitionStrategy PartitionStrategy { get; }

    /// <summary>
    /// Gets a value indicating whether the layout splits a pair's rows across multiple files.
    /// </summary>
    /// <value><see langword="true" /> when the layout is partitioned; otherwise <see langword="false" />.</value>
    public bool IsPartitioned => PartitionStrategy.IsPartitioned;

    /// <summary>
    /// Creates a custom layout from a partition strategy and optional directory and file-name delegates.
    /// </summary>
    /// <param name="partitionStrategy">The strategy deciding how a pair is split across files by date.</param>
    /// <param name="directory">
    /// An optional delegate resolving the directory that holds a pair's file or files. When <see langword="null" />, a
    /// default is used: <c>&lt;root&gt;/&lt;provider&gt;</c> for a single-file strategy, or
    /// <c>&lt;root&gt;/&lt;provider&gt;/&lt;From&gt;&lt;To&gt;</c> for a partitioned strategy.
    /// </param>
    /// <param name="fileName">
    /// An optional delegate resolving the name of a single cache file. When <see langword="null" />, a default is used:
    /// <c>&lt;From&gt;&lt;To&gt;.ext</c> for a single-file strategy, or <c>&lt;partitionKey&gt;.ext</c> for a
    /// partitioned strategy.
    /// </param>
    /// <returns>A layout driven by the supplied strategy and delegates.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="partitionStrategy" /> is <see langword="null" />.
    /// </exception>
    public static ExchangeRateCacheFileLayout Create(
        ExchangeRateCachePartitionStrategy partitionStrategy,
        Func<ExchangeRateCacheDirectoryContext, string>? directory = null,
        Func<ExchangeRateCacheFileContext, string>? fileName = null)
    {
        ThrowHelper.ThrowIfNull(partitionStrategy);

        return new ExchangeRateCacheFileLayout(
            partitionStrategy,
            directory ?? DefaultDirectory(partitionStrategy.IsPartitioned),
            fileName ?? DefaultFileName(partitionStrategy.IsPartitioned));
    }

    /// <summary>
    /// Resolves the directory that holds the supplied pair's file or files.
    /// </summary>
    /// <param name="root">The resolved cache root directory.</param>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The directory the pair's file or files are stored in.</returns>
    public string ResolveDirectory(string root, string provider, CurrencyPair pair) =>
        _directory(new ExchangeRateCacheDirectoryContext(root, provider, pair));

    /// <summary>
    /// Resolves the name of the file that holds the supplied pair and partition key.
    /// </summary>
    /// <param name="provider">The provider the cache is bound to.</param>
    /// <param name="pair">The currency pair.</param>
    /// <param name="partitionKey">The partition key, or the empty string for a single-file layout.</param>
    /// <param name="fileExtension">The file extension, including the leading period.</param>
    /// <returns>The file name, without directory.</returns>
    public string ResolveFileName(string provider, CurrencyPair pair, string partitionKey, string fileExtension) =>
        _fileName(new ExchangeRateCacheFileContext(provider, pair, partitionKey, fileExtension));

    /// <summary>
    /// Maps a path segment to a safe form by replacing characters that are illegal in a file name with an underscore.
    /// </summary>
    /// <param name="segment">The segment to sanitize.</param>
    /// <returns>The segment with any illegal characters replaced by an underscore.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="segment" /> is <see langword="null" />.
    /// </exception>
    public static string Sanitize(string segment)
    {
        ThrowHelper.ThrowIfNull(segment);

        if (segment.AsSpan().IndexOfAny(s_invalidFileNameChars) < 0)
            return segment;

        char[] chars = segment.ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            if (Array.IndexOf(s_invalidFileNameChars, chars[i]) >= 0)
                chars[i] = '_';
        }

        return new string(chars);
    }

    /// <summary>
    /// Builds the default <c>&lt;From&gt;&lt;To&gt;</c> token identifying a pair in a path.
    /// </summary>
    /// <param name="pair">The currency pair.</param>
    /// <returns>The concatenated currency codes, for example <c>AUDUSD</c>.</returns>
    private static string PairToken(CurrencyPair pair) =>
        $"{pair.From}{pair.To}";

    /// <summary>
    /// Builds a partitioned layout that isolates each pair in its own folder and names files by partition key.
    /// </summary>
    /// <param name="partitionStrategy">The partition strategy.</param>
    /// <returns>The partitioned layout.</returns>
    private static ExchangeRateCacheFileLayout Partitioned(ExchangeRateCachePartitionStrategy partitionStrategy) =>
        new(partitionStrategy, DefaultDirectory(isPartitioned: true), DefaultFileName(isPartitioned: true));

    /// <summary>
    /// Returns the default directory delegate for the supplied partitioning mode.
    /// </summary>
    /// <param name="isPartitioned">Whether the layout splits a pair across files.</param>
    /// <returns>The default directory delegate.</returns>
    private static Func<ExchangeRateCacheDirectoryContext, string> DefaultDirectory(bool isPartitioned) =>
        isPartitioned
            ? static ctx => Path.Combine(ctx.Root, Sanitize(ctx.Provider), Sanitize(PairToken(ctx.Pair)))
            : static ctx => Path.Combine(ctx.Root, Sanitize(ctx.Provider));

    /// <summary>
    /// Returns the default file-name delegate for the supplied partitioning mode.
    /// </summary>
    /// <param name="isPartitioned">Whether the layout splits a pair across files.</param>
    /// <returns>The default file-name delegate.</returns>
    private static Func<ExchangeRateCacheFileContext, string> DefaultFileName(bool isPartitioned) =>
        isPartitioned
            ? static ctx => Sanitize(ctx.PartitionKey) + ctx.FileExtension
            : static ctx => Sanitize(PairToken(ctx.Pair)) + ctx.FileExtension;
}
