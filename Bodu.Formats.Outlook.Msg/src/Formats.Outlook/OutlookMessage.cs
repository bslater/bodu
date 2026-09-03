// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMessage.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Msg;
using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook;

/// <summary>
/// Provides a disposable, read-only session over an Outlook message (<c>.msg</c> / MS-OXMSG), exposing every decoded
/// MAPI property together with curated conveniences for the common message fields.
/// </summary>
/// <remarks>
/// <para>
/// A <c>.msg</c> file is an OLE2 compound file; opening a message opens the container through
/// <see cref="CompoundFile" /> and decodes the root property stream eagerly, so the property surface is available
/// without further I/O. The session owns the container and, unless the caller opts to leave it open, the source stream;
/// dispose the message when reading is complete.
/// </para>
/// <para>
/// Every property is reachable through <see cref="Properties" />; the scalar conveniences (<see cref="Subject" />,
/// <see cref="SenderName" />, the time stamps, and the rest) are lazy views over that collection and return
/// <see langword="null" /> when the underlying property is absent.
/// </para>
/// <example>
/// <code language="csharp">
///<![CDATA[
/// using Bodu.Formats.Outlook;
///
/// using var message = OutlookMessage.OpenRead("invoice.msg");
///
/// Console.WriteLine(message.Subject);
/// Console.WriteLine(message.SenderEmailAddress);
///]]>
/// </code>
/// </example>
/// </remarks>
public sealed partial class OutlookMessage
    : IDisposable
{
    /// <summary>The open compound-file container, shared by nested messages.</summary>
    private readonly CompoundFile _compound;

    /// <summary>The storage this message reads from (the root, or an embedded-message storage).</summary>
    private readonly CompoundStorage _storage;

    /// <summary>The reader options the session was opened with.</summary>
    private readonly OutlookMessageReaderOptions _options;

    /// <summary>The decoded property collection.</summary>
    private readonly MapiPropertyCollection _properties;

    /// <summary>The decoded property-stream header (recipient and attachment counts).</summary>
    private readonly MsgPropertyStreamHeader _header;

    /// <summary>Whether this instance owns the container and disposes it.</summary>
    private readonly bool _ownsContainer;

    /// <summary>The encoding used to decode this message's code-page strings.</summary>
    private readonly System.Text.Encoding _stringEncoding;

    /// <summary>The root session, or <see langword="null" /> when this instance is the root.</summary>
    private readonly OutlookMessage? _root;

    /// <summary>The embedded-message nesting depth: zero for the root, one more per level opened from an attachment.</summary>
    private readonly int _depth;

    /// <summary>Whether this message has been disposed.</summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="OutlookMessage" /> class.
    /// </summary>
    /// <param name="compound">The open container.</param>
    /// <param name="storage">The storage the message reads from.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="properties">The decoded property collection.</param>
    /// <param name="header">The decoded property-stream header.</param>
    /// <param name="ownsContainer">Whether this instance owns and disposes the container.</param>
    /// <param name="stringEncoding">The encoding this message's code-page strings were decoded with.</param>
    /// <param name="root">
    /// The root session for a nested message, or <see langword="null" /> for the root itself.
    /// </param>
    /// <param name="depth">The embedded-message nesting depth; zero for the root.</param>
    internal OutlookMessage(
        CompoundFile compound,
        CompoundStorage storage,
        OutlookMessageReaderOptions options,
        MapiPropertyCollection properties,
        MsgPropertyStreamHeader header,
        bool ownsContainer,
        System.Text.Encoding stringEncoding,
        OutlookMessage? root,
        int depth = 0)
    {
        _compound = compound;
        _storage = storage;
        _options = options;
        _properties = properties;
        _header = header;
        _ownsContainer = ownsContainer;
        _stringEncoding = stringEncoding;
        _root = root;
        _depth = depth;
    }

    /// <summary>
    /// Gets the embedded-message nesting depth of this session: zero for a message opened from a file or stream, one
    /// more for each level opened through <see cref="OutlookAttachment.OpenMessage" />.
    /// </summary>
    /// <value>The nesting depth.</value>
    public int EmbeddedDepth =>
        _depth;

    /// <summary>
    /// Gets every decoded MAPI property of the message.
    /// </summary>
    /// <value>The tag-addressed property collection.</value>
    /// <exception cref="ObjectDisposedException">The message has been disposed.</exception>
    public MapiPropertyCollection Properties
    {
        get
        {
            ThrowIfDisposed();

            return _properties;
        }
    }

    /// <summary>
    /// Gets the message subject.
    /// </summary>
    /// <value>The <c>PidTagSubject</c> value, or <see langword="null" /> when absent.</value>
    public string? Subject =>
        Properties.GetString(MapiPropertyIds.Subject);

    /// <summary>
    /// Gets the sender display name.
    /// </summary>
    /// <value>The <c>PidTagSenderName</c> value, or <see langword="null" /> when absent.</value>
    public string? SenderName =>
        Properties.GetString(MapiPropertyIds.SenderName);

    /// <summary>
    /// Gets the sender email address.
    /// </summary>
    /// <value>The <c>PidTagSenderEmailAddress</c> value, or <see langword="null" /> when absent.</value>
    public string? SenderEmailAddress =>
        Properties.GetString(MapiPropertyIds.SenderEmailAddress);

    /// <summary>
    /// Gets the message class (for example, <c>IPM.Note</c>).
    /// </summary>
    /// <value>The <c>PidTagMessageClass</c> value, or <see langword="null" /> when absent.</value>
    public string? MessageClass =>
        Properties.GetString(MapiPropertyIds.MessageClass);

    /// <summary>
    /// Gets the internet message identifier.
    /// </summary>
    /// <value>The <c>PidTagInternetMessageId</c> value, or <see langword="null" /> when absent.</value>
    public string? InternetMessageId =>
        Properties.GetString(MapiPropertyIds.InternetMessageId);

    /// <summary>
    /// Gets the transport message headers.
    /// </summary>
    /// <value>The <c>PidTagTransportMessageHeaders</c> value, or <see langword="null" /> when absent.</value>
    public string? TransportMessageHeaders =>
        Properties.GetString(MapiPropertyIds.TransportMessageHeaders);

    /// <summary>
    /// Gets the time the message was submitted by the sending client.
    /// </summary>
    /// <value>The <c>PidTagClientSubmitTime</c> value, or <see langword="null" /> when absent.</value>
    public DateTimeOffset? SentTime =>
        Properties.GetDateTime(MapiPropertyIds.ClientSubmitTime);

    /// <summary>
    /// Gets the time the message was delivered.
    /// </summary>
    /// <value>The <c>PidTagMessageDeliveryTime</c> value, or <see langword="null" /> when absent.</value>
    public DateTimeOffset? ReceivedTime =>
        Properties.GetDateTime(MapiPropertyIds.MessageDeliveryTime);

    /// <summary>
    /// Opens a message from a file path.
    /// </summary>
    /// <param name="path">The path of the <c>.msg</c> file.</param>
    /// <returns>The opened message session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="path" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OutlookMsgFormatException">The file is not a compound file or not a valid message.</exception>
    public static OutlookMessage OpenRead(string path)
    {
        ThrowHelper.ThrowIfNull(path);

        FileStream stream = File.OpenRead(path);
        try
        {
            return Open(stream, OutlookMessageReaderOptions.Default, leaveOpen: false);
        }
        catch
        {
            stream.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Opens a message from a stream with default options.
    /// </summary>
    /// <param name="stream">The readable, seekable source stream positioned at the container start.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open when the message is disposed.</param>
    /// <returns>The opened message session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OutlookMsgFormatException">
    /// The stream is not a compound file or not a valid message.
    /// </exception>
    public static OutlookMessage OpenRead(Stream stream, bool leaveOpen = false) =>
        Open(stream, OutlookMessageReaderOptions.Default, leaveOpen);

    /// <summary>
    /// Opens a message from a stream with explicit options.
    /// </summary>
    /// <param name="stream">The readable, seekable source stream positioned at the container start.</param>
    /// <param name="options">The reader options.</param>
    /// <param name="leaveOpen"><see langword="true" /> to leave the stream open when the message is disposed.</param>
    /// <returns>The opened message session.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> or <paramref name="options" /> is <see langword="null" />.
    /// </exception>
    /// <exception cref="OutlookMsgFormatException">
    /// The stream is not a compound file, the container is malformed, or the message is not valid.
    /// </exception>
    public static OutlookMessage Open(Stream stream, OutlookMessageReaderOptions options, bool leaveOpen = false)
    {
        ThrowHelper.ThrowIfNull(stream);
        ThrowHelper.ThrowIfNull(options);

        CompoundFile compound;
        try
        {
            compound = CompoundFile.Open(stream, options.ToCompoundFileOptions(), leaveOpen);
        }
        catch (CompoundFileException ex)
        {
            throw new OutlookMsgFormatException(OutlookMsgResourceStrings.Format_Invalid_MsgNotCompound, ex);
        }

        try
        {
            MapiPropertyCollection properties = MsgPropertyDecoder.Decode(
                compound.RootStorage, MsgPropertyStreamKind.Root, options.ValidationLevel, inheritedEncoding: null, out MsgPropertyStreamHeader header);

            System.Text.Encoding encoding = MapiEncodingResolver.Resolve(properties, inherited: null);
            return new OutlookMessage(compound, compound.RootStorage, options, properties, header, ownsContainer: true, encoding, root: null);
        }
        catch
        {
            compound.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Determines whether a stream carries an Outlook message: an OLE2 compound file whose root storage holds the
    /// message property stream.
    /// </summary>
    /// <param name="stream">The readable, seekable stream to sniff; its position is restored before returning.</param>
    /// <returns><see langword="true" /> when the stream looks like a <c>.msg</c> file.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="stream" /> is <see langword="null" />.
    /// </exception>
    /// <remarks>
    /// The check does not require the conventional root class identifier — real-world writers frequently omit it — so
    /// the presence of the <c>__properties_version1.0</c> stream is the discriminator.
    /// </remarks>
    public static bool IsMsgFile(Stream stream)
    {
        ThrowHelper.ThrowIfNull(stream);

        if (!stream.CanRead || !stream.CanSeek)
            return false;

        long position = stream.Position;
        try
        {
            if (!CompoundFile.IsCompoundFile(stream))
                return false;

            stream.Position = position;
            var options = new CompoundFileOptions { ValidationLevel = CompoundValidationLevel.Minimal };
            using var compound = CompoundFile.Open(stream, options, leaveOpen: true);
            if (compound.RootStorage.TryOpenStream(MsgStreamNames.PropertiesStreamName, out CompoundStream? properties))
            {
                properties.Dispose();
                return true;
            }

            return false;
        }
        catch (CompoundFileException)
        {
            return false;
        }
        finally
        {
            stream.Position = position;
        }
    }

    /// <summary>
    /// Releases the container and, unless it was left open, the source stream. Disposing a nested message obtained from
    /// an attachment is a no-op — the root session owns the container, and the nested session's decoded properties stay
    /// readable until the root is disposed, after which every nested session throws
    /// <see cref="ObjectDisposedException" /> as well.
    /// </summary>
    public void Dispose()
    {
        if (_disposed || !_ownsContainer)
            return;

        _disposed = true;
        _compound.Dispose();
    }

    /// <summary>
    /// Throws when this session, or the root session a nested message belongs to, has been disposed.
    /// </summary>
    /// <exception cref="ObjectDisposedException">The session has been disposed.</exception>
    internal void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(_root?._disposed ?? _disposed, this);
}
