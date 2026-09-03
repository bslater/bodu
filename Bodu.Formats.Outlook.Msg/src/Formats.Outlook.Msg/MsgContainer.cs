// ---------------------------------------------------------------------------------------------------------------
// <copyright file="MsgContainer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.IO.Compound;

namespace Bodu.Formats.Outlook.Msg;

/// <summary>
/// Mediates every access to the compound-file container so that a container-level failure surfaces as the reader's
/// own <see cref="OutlookMsgFormatException" /> rather than a <see cref="CompoundFileException" />.
/// </summary>
/// <remarks>
/// A corrupt directory, FAT chain, or stream can fail at any point after the container opened — when a storage is
/// enumerated, a stream is looked up, or its bytes are read. Routing those calls through this type keeps the
/// documented exception contract: callers observe <see cref="OutlookFormatException" /> descendants only, with the
/// container exception preserved as the inner exception.
/// </remarks>
internal static class MsgContainer
{
    /// <summary>
    /// Attempts to open a child stream of a storage.
    /// </summary>
    /// <param name="storage">The parent storage.</param>
    /// <param name="name">The stream name.</param>
    /// <param name="stream">When this method returns <see langword="true" />, the opened stream.</param>
    /// <returns><see langword="true" /> when the stream exists.</returns>
    /// <exception cref="OutlookMsgFormatException">The container is malformed.</exception>
    internal static bool TryOpenStream(CompoundStorage storage, string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out CompoundStream stream)
    {
        try
        {
            return storage.TryOpenStream(name, out stream);
        }
        catch (CompoundFileException ex)
        {
            throw Wrap(ex);
        }
    }

    /// <summary>
    /// Attempts to open a child storage of a storage.
    /// </summary>
    /// <param name="storage">The parent storage.</param>
    /// <param name="name">The child storage name.</param>
    /// <param name="child">When this method returns <see langword="true" />, the child storage.</param>
    /// <returns><see langword="true" /> when the child exists.</returns>
    /// <exception cref="OutlookMsgFormatException">The container is malformed.</exception>
    internal static bool TryOpenStorage(CompoundStorage storage, string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out CompoundStorage child)
    {
        try
        {
            return storage.TryOpenStorage(name, out child);
        }
        catch (CompoundFileException ex)
        {
            throw Wrap(ex);
        }
    }

    /// <summary>
    /// Attempts to read a child stream of a storage in full.
    /// </summary>
    /// <param name="storage">The parent storage.</param>
    /// <param name="name">The stream name.</param>
    /// <param name="bytes">When this method returns <see langword="true" />, the stream payload.</param>
    /// <returns><see langword="true" /> when the stream exists.</returns>
    /// <exception cref="OutlookMsgFormatException">The container is malformed.</exception>
    internal static bool TryReadStream(CompoundStorage storage, string name, [System.Diagnostics.CodeAnalysis.MaybeNullWhen(false)] out byte[] bytes)
    {
        try
        {
            if (!storage.TryOpenStream(name, out CompoundStream? stream))
            {
                bytes = null;
                return false;
            }

            using (stream)
                bytes = stream.ReadAllBytes();

            return true;
        }
        catch (CompoundFileException ex)
        {
            throw Wrap(ex);
        }
    }

    /// <summary>
    /// Lists the child storages of a storage.
    /// </summary>
    /// <param name="storage">The parent storage.</param>
    /// <returns>The child storages, in directory order.</returns>
    /// <exception cref="OutlookMsgFormatException">The container is malformed.</exception>
    internal static List<CompoundStorage> GetStorages(CompoundStorage storage)
    {
        try
        {
            return [.. storage.EnumerateStorages()];
        }
        catch (CompoundFileException ex)
        {
            throw Wrap(ex);
        }
    }

    /// <summary>
    /// Wraps a container exception in the reader's format exception.
    /// </summary>
    /// <param name="exception">The container exception.</param>
    /// <returns>The exception to throw.</returns>
    internal static OutlookMsgFormatException Wrap(CompoundFileException exception) =>
        new(OutlookMsgResourceStrings.Format_Invalid_MsgContainer, exception);
}
