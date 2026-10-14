// ---------------------------------------------------------------------------------------------------------------
// <copyright file="OutlookMailStore.ContainerFaults.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using Bodu.Formats.Outlook.Pst;
using Bodu.IO.Pst;

namespace Bodu.Formats.Outlook;

public sealed partial class OutlookMailStore
{
    /// <summary>The container exception the <see cref="OutlookMailStoreReaderOptions.ValueOmittedCallback" /> last threw, which the translation of container faults lets through unchanged.</summary>
    private PstFileException? _callbackException;

    /// <summary>
    /// Determines whether a container exception caught at a public member is a fault of this store's container, which
    /// the member translates, rather than one the omission callback threw itself, which propagates unchanged.
    /// </summary>
    /// <param name="exception">The caught container exception.</param>
    /// <returns><see langword="true" /> when the exception is to be translated.</returns>
    internal bool IsContainerFault(PstFileException exception) =>
        !ReferenceEquals(exception, _callbackException);

    /// <summary>
    /// Wraps a lazily evaluated sequence so that a container fault raised while it is enumerated surfaces as
    /// <see cref="OutlookPstFormatException" />.
    /// </summary>
    /// <typeparam name="T">The element type.</typeparam>
    /// <param name="source">The sequence whose enumeration reads the container.</param>
    /// <returns>The same elements, in the same order, with container faults translated.</returns>
    /// <exception cref="OutlookPstFormatException">The container is malformed.</exception>
    internal IEnumerable<T> GuardContainer<T>(IEnumerable<T> source)
    {
        using IEnumerator<T> enumerator = source.GetEnumerator();
        while (true)
        {
            T current;
            try
            {
                if (!enumerator.MoveNext())
                    yield break;

                current = enumerator.Current;
            }
            catch (PstFileException ex) when (IsContainerFault(ex))
            {
                throw PstContainer.Wrap(ex);
            }

            yield return current;
        }
    }
}
