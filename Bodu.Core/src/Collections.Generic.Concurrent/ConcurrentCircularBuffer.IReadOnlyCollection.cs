// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ConcurrentCircularBuffer.IReadOnlyCollection.cs" company="PlaceholderCompany">
//     Copyright (c) PlaceholderCompany. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Bodu.Collections.Generic.Concurrent;

public sealed partial class ConcurrentCircularBuffer<T> :
    System.Collections.Generic.IReadOnlyCollection<T>
{
}
