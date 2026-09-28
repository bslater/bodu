// ---------------------------------------------------------------------------------------------------------------
// <copyright file="ChaCha20Core.StateBuffer.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace Bodu.Security.Cryptography;

internal static partial class ChaCha20Core
{
    /// <summary>
    /// Holds a sixteen-word ChaCha20 or Salsa20 state inline, inside the value that contains it, so that a keystream
    /// kept on the stack never touches the heap.
    /// </summary>
    [InlineArray(StateWords)]
    internal struct StateBuffer
    {
        /// <summary>The first word; the <see cref="InlineArrayAttribute" /> expands it to sixteen.</summary>
        private uint _word0;
    }
}
