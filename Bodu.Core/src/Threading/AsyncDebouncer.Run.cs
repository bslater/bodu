// ---------------------------------------------------------------------------------------------------------------
// <copyright file="AsyncDebouncer.Run.cs" company="Bodu Pty. Ltd.">
// Copyright (c) Bodu Pty. Ltd. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------------------------------------------

namespace Bodu.Threading;

public sealed partial class AsyncDebouncer
{
    /// <summary>
    /// Represents a single in-flight callback invocation, pairing the task that runs it with the cancellation source
    /// that controls it.
    /// </summary>
    private sealed class Run
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Run" /> class.
        /// </summary>
        /// <param name="cts">The cancellation source whose token is passed to the callback.</param>
        internal Run(CancellationTokenSource cts)
        {
            Cts = cts;
        }

        /// <summary>
        /// Gets the cancellation source whose token is observed by this invocation's callback.
        /// </summary>
        /// <value>The cancellation source for this run.</value>
        internal CancellationTokenSource Cts { get; }

        /// <summary>
        /// Gets or sets the task that represents this invocation's execution.
        /// </summary>
        /// <value>The running task, or <see langword="null" /> until it has been assigned.</value>
        internal Task? Task { get; set; }
    }
}
