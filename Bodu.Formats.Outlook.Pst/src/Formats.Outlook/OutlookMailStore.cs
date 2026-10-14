// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStore.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Text;
using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Provides a disposable, read-only session over an Outlook personal-folders mail store (a <c>.pst</c> file, MS-PST
/// Unicode or ANSI format): the store properties, the folder hierarchy, and the messages within it, decoded into the
/// shared MAPI value model.
/// </summary>
/// <remarks>
/// <para>
/// The session owns its <see cref="PstFile" /> container (and the source stream unless it is left open); every
/// <see cref="OutlookMailFolder" /> and <see cref="OutlookMailMessage" /> obtained from it is a view bound to the
/// session's lifetime - disposing the store invalidates them all. Reads are lazy and streaming-first: opening parses
/// only the container header, folder and message enumerations stream table rows, and each object decodes its properties
/// once on first access.
/// </para>
/// <para>
/// The session is single-threaded, matching the container's documented contract: its members - and the members of every
/// view obtained from it - must not be called concurrently.
/// </para>
/// <para>
/// Every failure to read the store surfaces as <see cref="OutlookPstFormatException" />, part of the
/// <see cref="OutlookFormatException" /> family the <c>.msg</c> reader shares. Container corruption met by the session
/// or any view - when the store opens, a property collection decodes, a table is enumerated, or a content stream is
/// read - is wrapped, with the container's <see cref="PstFileException" /> (and its
/// <see cref="PstFileException.Error" /> category) preserved as the inner exception.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Formats.Outlook;
///
/// using var store = OutlookMailStore.OpenRead("archive.pst");
///
/// foreach (OutlookMailFolder folder in store.RootFolder.EnumerateSubfolders())
/// {
///     Console.WriteLine(folder.DisplayName);
///     foreach (OutlookMailMessage message in folder.EnumerateMessages())
///         Console.WriteLine($"  {message.Subject} - {message.SenderName}");
/// }
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed partial class OutlookMailStore
    : IDisposable
{
    /// <summary>The owned container session.</summary>
    private readonly PstFile _file;

    /// <summary>The reader options the store was opened with.</summary>
    private readonly OutlookMailStoreReaderOptions _options;

    /// <summary>The lazily decoded store properties.</summary>
    private MapiPropertyCollection? _properties;

    /// <summary>The encoding the store's code-page strings decoded with; set when <see cref="Properties" /> decodes.</summary>
    private Encoding? _storeEncoding;

    /// <summary>The lazily created root folder view.</summary>
    private OutlookMailFolder? _rootFolder;

    /// <summary>Whether this session has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMailStore" /> class.
    /// </summary>
    /// <param name="file">The owned container session.</param>
    /// <param name="options">The reader options.</param>
    private OutlookMailStore(PstFile file, OutlookMailStoreReaderOptions options)
    {
        _file = file;
        _options = options;
    }

    /// <summary>
    /// Gets every decoded property of the store object.
    /// </summary>
    /// <value>The tag-addressed property collection, decoded once on first access.</value>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// The container is malformed, or, under strict validation, a property value cannot be decoded.
    /// </exception>
    /// <remarks>
    /// An exception thrown by <see cref="OutlookMailStoreReaderOptions.ValueOmittedCallback" /> while the properties
    /// are decoded propagates from this access unchanged.
    /// </remarks>
    public MapiPropertyCollection Properties
    {
        get
        {
            ThrowIfDisposed();

            if (_properties is null)
            {
                try
                {
                    // A store without its message-store object still has a readable folder tree; its strings decode
                    // under the default code page.
                    if (_file.TryGetNode(PstNodeId.MessageStore, out PstNode? storeNode))
                    {
                        _properties = PstMapiPropertyReader.Read(
                            storeNode.ReadPropertyContext(),
                            inheritedEncoding: null,
                            Strict,
                            CreateOmissionReporter(storeNode.NodeId),
                            out Encoding encoding);
                        _storeEncoding = encoding;
                    }
                    else
                    {
                        _properties = MapiPropertyCollection.Empty;
                        _storeEncoding = MapiEncodingResolver.GetEncoding(null, null);
                    }
                }
                catch (PstFileException ex) when (IsContainerFault(ex))
                {
                    throw PstContainer.Wrap(ex);
                }
            }

            return _properties;
        }
    }

    /// <summary>
    /// Gets the store display name.
    /// </summary>
    /// <value>The <c>PidTagDisplayName</c> value, or <see langword="null" /> when absent.</value>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="OutlookPstFormatException">The container is malformed.</exception>
    public string? DisplayName =>
        Properties.GetString(MapiPropertyIds.DisplayName);

    /// <summary>
    /// Gets the root of the store's folder hierarchy.
    /// </summary>
    /// <value>The root folder view, created once on first access.</value>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="OutlookPstFormatException">The container is malformed, or has no root folder node.</exception>
    /// <remarks>
    /// The root folder itself is structural - user folders (the IPM subtree) hang beneath it. Walk
    /// <see cref="OutlookMailFolder.EnumerateSubfolders" /> to reach them.
    /// </remarks>
    public OutlookMailFolder RootFolder
    {
        get
        {
            ThrowIfDisposed();

            if (_rootFolder is null)
            {
                try
                {
                    _rootFolder = new OutlookMailFolder(this, _file.GetNode(PstNodeId.RootFolder));
                }
                catch (PstFileException ex) when (IsContainerFault(ex))
                {
                    throw PstContainer.Wrap(ex);
                }
            }

            return _rootFolder;
        }
    }

    /// <summary>
    /// Gets a value indicating whether the session applies strict validation to messaging structures.
    /// </summary>
    /// <value><see langword="true" /> under <see cref="PstValidationLevel.Strict" />.</value>
    internal bool Strict =>
        _options.ValidationLevel == PstValidationLevel.Strict;

    /// <summary>
    /// Creates the reporter that passes an object's omitted values to the configured
    /// <see cref="OutlookMailStoreReaderOptions.ValueOmittedCallback" />.
    /// </summary>
    /// <param name="nodeId">The node whose property context or table holds the object's values.</param>
    /// <param name="rowId">The table row holding the values, or <see langword="null" /> for a property context.</param>
    /// <returns>The reporter, or <see langword="null" /> when no callback is configured.</returns>
    /// <remarks>
    /// A <see cref="PstFileException" /> the callback throws is the caller's own, not a fault of this store's
    /// container, so the reporter records it and the public members, which translate container faults, let it through
    /// unchanged.
    /// </remarks>
    internal Action<ushort, OutlookOmittedValueReason>? CreateOmissionReporter(PstNodeId nodeId, uint? rowId = null)
    {
        Action<OutlookOmittedValue>? callback = _options.ValueOmittedCallback;
        if (callback is null)
            return null;

        return (propertyId, reason) =>
        {
            try
            {
                callback(new OutlookOmittedValue(nodeId, rowId, propertyId, reason));
            }
            catch (PstFileException ex)
            {
                _callbackException = ex;
                throw;
            }
        };
    }

    /// <summary>
    /// Gets a value indicating whether compressed RTF bodies are decompressed by the body conveniences.
    /// </summary>
    /// <value>The configured <see cref="OutlookMailStoreReaderOptions.DecompressRtf" /> value.</value>
    internal bool DecompressRtf =>
        _options.DecompressRtf;

    /// <summary>
    /// Gets the deepest embedded-message nesting the session opens.
    /// </summary>
    /// <value>The configured <see cref="OutlookMailStoreReaderOptions.MaxEmbeddedMessageDepth" /> value.</value>
    internal int MaxEmbeddedMessageDepth =>
        _options.MaxEmbeddedMessageDepth;

    /// <summary>
    /// Gets the largest decompressed RTF body the session produces.
    /// </summary>
    /// <value>The configured <see cref="OutlookMailStoreReaderOptions.MaxDecompressedRtfBytes" /> value.</value>
    internal int MaxDecompressedRtfBytes =>
        _options.MaxDecompressedRtfBytes;

    /// <summary>
    /// Gets the largest by-value attachment payload decoded into an attachment's property collection.
    /// </summary>
    /// <value>
    /// The <see cref="OutlookMailStoreReaderOptions.MaxInlineAttachmentBytes" /> the session was opened with.
    /// </value>
    internal int MaxInlineAttachmentBytes =>
        _options.MaxInlineAttachmentBytes;

    /// <summary>
    /// Gets the encoding the store's code-page strings decoded with, forcing the store properties to decode first.
    /// </summary>
    /// <value>The store-level encoding child objects inherit when they declare no code page of their own.</value>
    internal Encoding StoreEncoding
    {
        get
        {
            _ = Properties;

            return _storeEncoding!;
        }
    }

    /// <summary>
    /// Opens a mail store from a path with default options.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>The open session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException"><paramref name="path" /> is empty.</exception>
    /// <exception cref="FileNotFoundException">No file exists at <paramref name="path" />.</exception>
    /// <exception cref="DirectoryNotFoundException">A directory in <paramref name="path" /> does not exist.</exception>
    /// <exception cref="UnauthorizedAccessException">
    /// <paramref name="path" /> names a directory, or the caller does not have permission to read the file.
    /// </exception>
    /// <exception cref="IOException">The file could not be opened.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// The file is not a structurally valid PST file, or is a format variant or uses a content encoding the reader does
    /// not support; the container's <see cref="PstFileFormatException" /> or
    /// <see cref="PstUnsupportedFormatException" /> is the inner exception.
    /// </exception>
    public static OutlookMailStore OpenRead(string path)
    {
        ThrowHelper.ThrowIfNull(path);

        FileStream stream = File.OpenRead(path);
        try
        {
            return Open(stream, OutlookMailStoreReaderOptions.Default, leaveOpen: false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Opens a mail store from a stream with default options.
    /// </summary>
    /// <param name="stream">The readable, seekable stream; the file is read from its current position.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open when the session is disposed.</param>
    /// <returns>The open session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">The stream is not readable and seekable.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// The stream is not a structurally valid PST file, or is a format variant or uses a content encoding the reader
    /// does not support; the container's <see cref="PstFileFormatException" /> or
    /// <see cref="PstUnsupportedFormatException" /> is the inner exception.
    /// </exception>
    public static OutlookMailStore OpenRead(Stream stream, bool leaveOpen = false) =>
        Open(stream, OutlookMailStoreReaderOptions.Default, leaveOpen);

    /// <summary>
    /// Opens a mail store from a stream with explicit options.
    /// </summary>
    /// <param name="stream">The readable, seekable stream; the file is read from its current position.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open when the session is disposed.</param>
    /// <returns>The open session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="ArgumentException">The stream is not readable and seekable.</exception>
    /// <exception cref="OutlookPstFormatException">
    /// The stream is not a structurally valid PST file, or is a format variant or uses a content encoding the reader
    /// does not support; the container's <see cref="PstFileFormatException" /> or
    /// <see cref="PstUnsupportedFormatException" /> is the inner exception.
    /// </exception>
    public static OutlookMailStore Open(Stream stream, OutlookMailStoreReaderOptions options, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);

        PstFile file;
        try
        {
            file = PstFile.Open(stream, options.ToPstFileOptions(), leaveOpen);
        }
        catch (PstFileException ex)
        {
            throw PstContainer.Wrap(ex);
        }

        return new OutlookMailStore(file, options);
    }

    /// <summary>
    /// Determines whether a stream begins with the <c>!BDN</c> magic; its position is restored before returning.
    /// </summary>
    /// <param name="stream">The readable, seekable stream to sniff.</param>
    /// <returns>
    /// <see langword="true" /> when the stream begins with the magic every PST variant shares, the OST included,
    /// whether or not this reader opens it.
    /// </returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The check reads only the magic, so it never throws for malformed content; a stream that cannot be read and
    /// sought answers <see langword="false" />.
    /// </remarks>
    public static bool IsPstFile(Stream stream) =>
        PstFile.IsPstFile(stream);

    /// <summary>
    /// Releases the session and its container (and source stream, unless it was left open).
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        // The flag is set first so a view that observes a partially disposed session fails with the disposal
        // exception rather than a container error.
        _disposed = true;
        _file.Dispose();
    }

    /// <summary>
    /// Throws when the session has been disposed; every view bound to the session calls this before touching the
    /// container.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    internal void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_disposed, this);

    /// <summary>
    /// Retrieves a node from the owned container, guarding against a disposed session.
    /// </summary>
    /// <param name="id">The node identifier.</param>
    /// <returns>The node.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    /// <exception cref="PstNodeNotFoundException">No node with the identifier exists.</exception>
    internal PstNode GetNode(PstNodeId id)
    {
        ThrowIfDisposed();

        return _file.GetNode(id);
    }

    /// <summary>
    /// Attempts to retrieve a node from the owned container, guarding against a disposed session.
    /// </summary>
    /// <param name="id">The node identifier.</param>
    /// <param name="node">When this method returns <see langword="true" />, the node.</param>
    /// <returns><see langword="true" /> when the node exists.</returns>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    internal bool TryGetNode(PstNodeId id, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out PstNode node)
    {
        ThrowIfDisposed();

        return _file.TryGetNode(id, out node);
    }
}
