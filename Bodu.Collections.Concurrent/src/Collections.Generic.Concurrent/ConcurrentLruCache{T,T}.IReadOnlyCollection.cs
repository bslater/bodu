// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentLruCache{T,T}.IReadOnlyCollection.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Collections.Generic.Concurrent;

/// <content> Declares the <see cref="IReadOnlyCollection{T}" /> surface, satisfied by the public <see cref="Count" />
/// property. </content>
public sealed partial class ConcurrentLruCache<TKey, TValue> :
    IReadOnlyCollection<KeyValuePair<TKey, TValue>>
{
}
